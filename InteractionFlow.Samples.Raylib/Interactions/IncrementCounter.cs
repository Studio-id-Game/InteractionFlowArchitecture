using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Raylib.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.Interactions;

public sealed class IncrementCounter(
    ICounterOperation operation,
    ICounterReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction(exceptionPort, cancellationPort, operation, reaction)
{
    protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        => await reaction.RespondAsync(context, await operation.ReadAsync(context));
}
