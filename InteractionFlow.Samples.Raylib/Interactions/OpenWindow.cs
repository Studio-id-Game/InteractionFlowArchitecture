using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Raylib.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.Interactions;

public sealed class OpenWindow(
    IWindowReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction(exceptionPort, cancellationPort, reaction)
{
    protected override Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        => Task.FromResult(reaction.Open(context));
}
