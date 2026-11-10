using InteractionFlow.Samples.EscapeGame.Aspects.Shared.ExternalPorts;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.View.Interactions;

public sealed class OpenWindow(
    IWindowReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<IViewContext>(exceptionPort, cancellationPort, reaction)
{
    protected override ValueTask<ReactionEnd> ExecuteCoreAsync(IViewContext context)
        => new(reaction.Open(context));
}
