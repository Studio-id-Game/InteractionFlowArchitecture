using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.OperationPorts;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.Interactions
{
    internal sealed class OperateDoor(
        IDoorOperation operation,
        IDoorReaction reaction,
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort)
        : Interaction(exceptionPort, cancellationPort, operation, reaction)
    {
        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            var command = await operation.ReadCommandAsync(context);
            return await reaction.ReactAsync(context, command);
        }
    }
}
