using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Operations;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.OperationPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.Externals.Operations
{
    public sealed class ConsoleDoorOperation : Operation, IDoorOperation
    {
        public override void ForceResetMemoryState()
        {
        }

        public ValueTask<DoorCommand> ReadCommandAsync(IFlowContext context)
        {
            Console.Write("Door command (Open/Close, Enter to exit): ");
            var text = Console.ReadLine()?.Trim();

            return new(text?.ToUpperInvariant() switch
            {
                "OPEN" => DoorCommand.Open,
                "CLOSE" => DoorCommand.Close,
                "" or null => DoorCommand.Exit,
                _ => DoorCommand.Unknown,
            });
        }
    }
}
