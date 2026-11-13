using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;

// 相互作用内の段階とフロー間で共有する継続方針。更新の原子性はAccess側が担います。
internal static class GameplayCheckpoint
{
    internal static async ValueTask<bool> WaitToContinueAsync(IGameplayContext context, ReactionEnd end)
    {
        if (end.HasException || IsEnd(context))
            return false;

        try
        {
            var sessionId = context.SessionId;
            var cancellation = context.Cancellation;
            return await context.Gameplay.WaitForActiveAsync(sessionId, cancellation.GetToken()) && !IsEnd(context);
        }
        catch (OperationCanceledException) when (context.Lifetime.ShutdownRequested)
        {
            return false;
        }
    }

    private static bool IsEnd(IGameplayContext context)
    {
        return context.Gameplay.ReadGameState(context.SessionId).Info == ExecutionMode.Stopping;
    }
}
