using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;

// 相互作用内の段階とフロー間で共有する継続方針。更新の原子性はAccess側が担います。
internal static class GameplayCheckpoint
{
    internal static async ValueTask<bool> WaitToContinueAsync(IGameplayContext context, ReactionEnd end)
    {
        if (end.HasException || context.Gameplay.ReadGameState(context.SessionId).Info == ExecutionMode.Stopping)
            return false;

        try
        {
            return await context.Gameplay.WaitForActiveAsync(context.SessionId, context.Cancellation.GetToken())
                && context.Gameplay.ReadGameState(context.SessionId).Info != ExecutionMode.Stopping;
        }
        catch (OperationCanceledException) when (context.Lifetime.ShutdownRequested)
        {
            return false;
        }
    }
}
