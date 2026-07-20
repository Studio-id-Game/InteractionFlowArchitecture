using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.HelloDoor.Entities;
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
            if (!context.TryGet<DoorState>(out var door))
            {
                return await reaction.WriteAsync(context, "No door context.");
            }

            var command = await operation.ReadCommandAsync(context);

            return command switch
            {
                DoorCommand.Open when !door.IsOpen => await OpenAsync(),
                DoorCommand.Open => await reaction.WriteAsync(context, "The door is already open."),
                DoorCommand.Close when door.IsOpen => await CloseAsync(),
                DoorCommand.Close => await reaction.WriteAsync(context, "The door is already closed."),
                DoorCommand.Exit => await ExitAsync(),
                _ => await reaction.WriteAsync(context, "Use Open or Close."),
            };

            async Task<ReactionEnd> OpenAsync()
            {
                door.IsOpen = true;
                return await reaction.WriteAsync(context, "The door opens.");
            }

            async Task<ReactionEnd> CloseAsync()
            {
                door.IsOpen = false;
                return await reaction.WriteAsync(context, "The door closes.");
            }

            async Task<ReactionEnd> ExitAsync()
            {
                door.ExitRequested = true;
                return await reaction.WriteAsync(context, "Goodbye.");
            }
        }
    }
}
