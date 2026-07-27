using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Operations;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.OperationPorts;
using InteractionFlow.Samples.HelloDoor.Lock.Entities;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.Lock.Externals.Operations
{
    public sealed class ConsoleLockDoorOperation : Operation, IDoorOperation
    {
        public override void ForceResetMemoryState()
        {
        }

        public ValueTask<DoorCommand> ReadCommandAsync(IFlowContext context)
        {
            Console.Write("Door command (Open/Close/Lock/Unlock, Enter to exit): ");
            var text = Console.ReadLine()?.Trim();

            var result = text?.ToUpperInvariant() switch
            {
                "OPEN" => DoorCommand.Open,
                "CLOSE" => DoorCommand.Close,
                "" or null => DoorCommand.Exit,
                _ => DoorCommand.Unknown,
            };

            if (context.TryGet<RefEntry<DoorLockCommand>>(out var doorLock))
            {
                if (result == DoorCommand.Unknown)
                {
                    doorLock.Value = text?.ToUpperInvariant() switch
                    {
                        "LOCK" => DoorLockCommand.Lock,
                        "UNLOCK" => DoorLockCommand.Unlock,
                        _ => DoorLockCommand.Unknown,
                    };
                }
                else
                {
                    doorLock.Value = DoorLockCommand.Unknown;
                }
            }

            return new(result);
        }
    }
}
