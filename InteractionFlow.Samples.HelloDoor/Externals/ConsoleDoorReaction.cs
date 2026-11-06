using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.Externals
{
    public sealed class ConsoleDoorReaction : Reaction, IDoorReaction
    {
        public override void ForceResetMemoryState()
        {
        }

        public ValueTask<ReactionEnd> ReactDoorAsync(IFlowContext context, DoorCommand command)
        {
            var exception = ReactDoorValueAsync(context, command);
            var reactionEnd = GetEnd(exception);
            return ValueTask.FromResult(reactionEnd);
        }

        private static Exception? ReactDoorValueAsync(IFlowContext context, DoorCommand command)
        {
            if (!context.TryGet<DoorState>(out var door))
            {
                return new Exception("No door context.");
            }

            var message = door.ApplyCommand(command);
            Console.WriteLine(message);

            return null;
        }
    }
}
