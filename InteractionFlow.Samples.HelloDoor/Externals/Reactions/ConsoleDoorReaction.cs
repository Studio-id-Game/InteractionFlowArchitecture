using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Reactions;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.Externals.Reactions
{
    internal sealed class ConsoleDoorReaction : Reaction, IDoorReaction
    {
        public override void ForceResetMemoryState()
        {
        }

        public ValueTask<ReactionEnd> ReactAsync(IFlowContext context, DoorCommand command)
        {
            if (!context.TryGet<DoorState>(out var door))
            {
                Console.WriteLine("No door context.");
                return new(GetEnd());
            }

            Console.WriteLine(GetMessageAndUpdateState(door, command));
            return new(GetEnd());
        }

        private static string GetMessageAndUpdateState(DoorState door, DoorCommand command)
        {
            switch (command)
            {
                case DoorCommand.Open when !door.IsOpen:
                    door.IsOpen = true;
                    return "The door opens.";

                case DoorCommand.Open:
                    return "The door is already open.";

                case DoorCommand.Close when door.IsOpen:
                    door.IsOpen = false;
                    return "The door closes.";

                case DoorCommand.Close:
                    return "The door is already closed.";

                case DoorCommand.Exit:
                    door.ExitRequested = true;
                    return "Goodbye.";

                default:
                    return "Use Open or Close.";
            }
        }
    }
}
