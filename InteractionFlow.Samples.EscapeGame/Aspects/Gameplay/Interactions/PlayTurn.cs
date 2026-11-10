using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;

// 選択の開始から一手の確定、双方の行動と提示完了までを一つの目的にまとめます。
public sealed class PlayTurn(
    IGameInputOperation operation,
    IGameplayReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<IGameplayContext>(exceptionPort, cancellationPort, operation, reaction)
{
    protected override async ValueTask<ReactionEnd> ExecuteCoreAsync(IGameplayContext context)
    {
        var end = await reaction.BeginActionSelectionAsync(context);
        if (!await GameplayCheckpoint.WaitToContinueAsync(context, end))
            return end;

        while (!context.Gameplay.ReadGameState(context.SessionId).Confirmed)
        {
            var command = await operation.ReadCommandAsync(context);
            end = await reaction.HandleCommandAsync(context, command);
            if (!await GameplayCheckpoint.WaitToContinueAsync(context, end))
                return end;
        }

        end = await reaction.ApplyPlayerActionAsync(context);
        if (!await GameplayCheckpoint.WaitToContinueAsync(context, end)
            || context.Gameplay.ReadGameState(context.SessionId).Outcome != GameOutcome.Running)
        {
            return end;
        }

        return await reaction.ApplyEnemyActionAsync(context);
    }
}
