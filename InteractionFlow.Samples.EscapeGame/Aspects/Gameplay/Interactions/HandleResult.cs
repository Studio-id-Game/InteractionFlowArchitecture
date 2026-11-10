using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;

public sealed class HandleResult(
    IGameInputOperation operation,
    IGameplayReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<IGameplayContext>(exceptionPort, cancellationPort, operation, reaction)
{
    protected override async ValueTask<ReactionEnd> ExecuteCoreAsync(IGameplayContext context)
    {
        var end = await reaction.PresentResultAsync(context);
        if (!await GameplayCheckpoint.WaitToContinueAsync(context, end))
            return end;

        // 提示を終えてから受付を開き、リトライの意図を受け取るまで画面を保持します。
        end = await reaction.EnableResultInputAsync(context);
        if (!await GameplayCheckpoint.WaitToContinueAsync(context, end))
            return end;

        while (!context.Gameplay.ReadGameState(context.SessionId).RetryRequested)
        {
            var command = await operation.ReadCommandAsync(context);
            end = await reaction.HandleCommandAsync(context, command);
            if (!await GameplayCheckpoint.WaitToContinueAsync(context, end))
                return end;
        }

        return end;
    }
}
