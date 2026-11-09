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
    SelectAction select)
    : SystemFlow<EscapeApplicationContext>(begin, turn, show, select)
{
    protected override async Task<InteractionEnd> ExecuteCoreAsync(EscapeApplicationContext context)
    {
        var end = await begin.ExecuteAsync(context);
        if (!await GameFlowCheckpoint.CanContinueAsync(context, end))
            return end;

        while (context.Gameplay.GetProgress(context.SessionId).Outcome == GameOutcome.Running)
        {
            end = await turn.ExecuteAsync(context);
            if (!await GameFlowCheckpoint.CanContinueAsync(context, end))
                return end;
        }

        end = await show.ExecuteAsync(context);
        if (!await GameFlowCheckpoint.CanContinueAsync(context, end))
            return end;

        // ShowResultが提示と受付開始を完了してから、リトライ入力を待ちます。
        while (!context.Gameplay.GetProgress(context.SessionId).RetryRequested)
        {
            end = await select.ExecuteAsync(context);
            if (!await GameFlowCheckpoint.CanContinueAsync(context, end))
                return end;
        }

        return end;
    }
}
