using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;

// Selecting / Result のどちらも、一件の入力を受け、現在段階に応じた反応を返します。
public sealed class HandleGameInput(
    IGameInputOperation operation,
    IGameplayReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<IGameplayContext>(exceptionPort, cancellationPort, operation, reaction)
{
    protected override async ValueTask<ReactionEnd> ExecuteCoreAsync(IGameplayContext context)
    {
        var command = await operation.ReadCommandAsync(context);
        return await reaction.HandleCommandAsync(context, command);
    }
}
