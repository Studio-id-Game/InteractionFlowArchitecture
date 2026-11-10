using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.SystemFlows;

public sealed class GameTurnSystemFlow(
    BeginActionSelection enable,
    HandleGameInput select,
    ApplyPlayerAction player,
    ApplyEnemyAction enemy)
    : SystemFlow<IGameplayContext>(enable, select, player, enemy)
{
    protected override async Task<InteractionEnd> ExecuteCoreAsync(IGameplayContext context)
    {
        var end = await enable.ExecuteAsync(context);
        if (!await GameFlowCheckpoint.WaitToContinueAsync(context, end))
            return end;

        while (!context.Gameplay.ReadGameState(context.SessionId).Confirmed)
        {
            end = await select.ExecuteAsync(context);
            if (!await GameFlowCheckpoint.WaitToContinueAsync(context, end))
                return end;
        }

        end = await player.ExecuteAsync(context);
        if (!await GameFlowCheckpoint.WaitToContinueAsync(context, end)
            || context.Gameplay.ReadGameState(context.SessionId).Outcome != GameOutcome.Running)
        {
            return end;
        }

        return await enemy.ExecuteAsync(context);
    }
}
