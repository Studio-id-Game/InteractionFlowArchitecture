using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.SystemFlows;

public sealed class GameSessionSystemFlow(
    PlayTurn turn,
    HandleResult result)
    : SystemFlow<IGameplayContext>(turn, result)
{
    protected override async Task<InteractionEnd> ExecuteCoreAsync(IGameplayContext context)
    {
        while (context.Gameplay.ReadGameState(context.SessionId).State.Outcome == GameOutcome.Running)
        {
            var end = await turn.ExecuteAsync(context);
            if (!await GameplayCheckpoint.WaitToContinueAsync(context, end.End))
                return end;
        }

        return await result.ExecuteAsync(context);
    }
}
