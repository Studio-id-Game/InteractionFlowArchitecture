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
        if (end.HasException || context.Session.GetProgress(context.SessionId).Stopping)
            return false;

        try
        {
            return await context.Control.WaitForActiveAsync(context.SessionId, context.Cancellation.GetToken())
                && !context.Session.GetProgress(context.SessionId).Stopping;
        }
        catch (OperationCanceledException) when (context.Session.ShutdownRequested)
        {
            return false;
        }
    }
}
