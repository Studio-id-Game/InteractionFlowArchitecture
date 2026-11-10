using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;
using System;

namespace InteractionFlow.Samples.EscapeGame.SystemFlows;

// フロー間の共通方針。状態更新の原子性はAccess側の再確認が担います。
internal static class GameFlowCheckpoint
{
    internal static async ValueTask<bool> WaitToContinueAsync(IGameplayContext context, InteractionEnd end)
    {
        if (end.HasException || context.Gameplay.ReadGameState(context.SessionId).Mode == ExecutionMode.Stopping)
            return false;

        try
        {
            return await context.Gameplay.WaitForActiveAsync(context.SessionId, context.Cancellation.GetToken())
                && context.Gameplay.ReadGameState(context.SessionId).Mode != ExecutionMode.Stopping;
        }
        catch (OperationCanceledException) when (context.Lifetime.ShutdownRequested)
        {
            return false;
        }
    }
}
