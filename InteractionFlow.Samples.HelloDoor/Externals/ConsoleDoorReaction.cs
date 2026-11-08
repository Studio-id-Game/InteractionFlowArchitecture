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
            try
            {
                var exception = ReactDoorValueAsync(context, command);
                return ValueTask.FromResult(GetEnd(exception));
            }
            catch (RequiredContextValueNotFoundException exception)
            {
                return ValueTask.FromResult(GetEnd(exception));
            }
        }

        private static Exception? ReactDoorValueAsync(IFlowContext context, DoorCommand command)
        {
            var door = context.GetRequired<DoorState>();

            var message = door.ApplyCommand(command);
            Console.WriteLine(message);

            return null;
        }
    }
}
