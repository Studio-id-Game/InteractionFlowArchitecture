using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.SystemFlows;

// フロー間の共通方針。状態更新の原子性はAccess側の再確認が担います。
internal static class GameFlowCheckpoint
{
    internal static async ValueTask<bool> CanContinueAsync(EscapeApplicationContext context, InteractionEnd end)
    {
        if (end.HasException || context.Gameplay.GetProgress(context.SessionId).Stopping)
            return false;

        try
        {
            return await context.Gameplay.WaitForActiveAsync(context.SessionId, context.Cancellation.GetToken())
                && !context.Gameplay.GetProgress(context.SessionId).Stopping;
        }
        catch (OperationCanceledException) when (context.Lifetime.ShutdownRequested)
        {
            return false;
        }
    }
}
