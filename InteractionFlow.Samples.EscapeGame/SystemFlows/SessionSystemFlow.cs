using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Interactions;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.SystemFlows;

public sealed class SessionSystemFlow(
    BeginSession begin,
    TurnSystemFlow turn,
    ShowResult show,
    EnableResultInput enableResult,
    SelectAction select)
    : SystemFlow<EscapeApplicationContext>(begin, turn, show, enableResult, select)
{
    protected override async Task<InteractionEnd> ExecuteCoreAsync(EscapeApplicationContext context)
    {
        var end = await begin.ExecuteAsync(context);
        if (!await CanContinueAsync(context, end))
            return end;

        while (context.World.GetProgress(context.SessionId).Outcome == GameOutcome.Running)
        {
            end = await turn.ExecuteAsync(context);
            if (!await CanContinueAsync(context, end))
                return end;
        }

        end = await show.ExecuteAsync(context);
        if (!await CanContinueAsync(context, end))
            return end;

        // 結果画面の提示が完了するまで、リトライ入力待機は開始しません。
        end = await enableResult.ExecuteAsync(context);
        if (!await CanContinueAsync(context, end))
            return end;

        while (!context.World.GetProgress(context.SessionId).RetryRequested)
        {
            end = await select.ExecuteAsync(context);
            if (!await CanContinueAsync(context, end))
                return end;
        }

        return end;
    }

    private static async ValueTask<bool> CanContinueAsync(EscapeApplicationContext context, InteractionEnd end)
    {
        if (end.HasException || context.World.GetProgress(context.SessionId).Stopping)
            return false;

        try
        {
            return await context.Control.WaitForActiveAsync(context.SessionId, context.Cancellation.GetToken())
                && !context.World.GetProgress(context.SessionId).Stopping;
        }
        catch (System.OperationCanceledException) when (context.World.ShutdownRequested)
        {
            return false;
        }
    }
}
