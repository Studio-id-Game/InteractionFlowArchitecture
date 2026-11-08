using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.Interactions;

public sealed class PresentFrame(
    IWindowOperation operation,
    IWindowReaction reaction,
    ICounterOperation counter,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction(exceptionPort, cancellationPort, operation, reaction, counter)
{
    protected override Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
    {
        operation.Poll(context);

        var input = context.GetRequired<FrameState>();

        if (!input.CloseRequested && !input.CancelRequested && input.Command is CounterCommand command)
            counter.Submit(context, command);

        return Task.FromResult(reaction.Draw(context));
    }
}
