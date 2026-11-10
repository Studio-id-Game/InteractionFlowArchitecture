using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.SystemFlows;

public sealed class SessionSystemFlow(
    TurnSystemFlow turn,
    PresentResult show,
    HandleGameInput select)
    : SystemFlow<IGameplayContext>(turn, show, select)
{
    protected override async Task<InteractionEnd> ExecuteCoreAsync(IGameplayContext context)
    {
        InteractionEnd end;
        while (context.Gameplay.GetSessionProgress(context.SessionId).Outcome == GameOutcome.Running)
        {
            end = await turn.ExecuteAsync(context);
            if (!await GameFlowCheckpoint.WaitToContinueAsync(context, end))
                return end;
        }

        end = await show.ExecuteAsync(context);
        if (!await GameFlowCheckpoint.WaitToContinueAsync(context, end))
            return end;

        // PresentResultが提示と受付開始を完了してから、リトライ入力を待ちます。
        while (!context.Gameplay.GetSessionProgress(context.SessionId).RetryRequested)
        {
            end = await select.ExecuteAsync(context);
            if (!await GameFlowCheckpoint.WaitToContinueAsync(context, end))
                return end;
        }

        return end;
    }
}
