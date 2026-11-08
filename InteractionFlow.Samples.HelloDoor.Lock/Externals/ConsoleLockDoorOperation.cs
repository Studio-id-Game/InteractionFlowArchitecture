using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.ExternalPorts;
using InteractionFlow.Samples.HelloDoor.Lock.Entities;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.Lock.Externals
{
    public sealed class ConsoleLockDoorOperation : Operation, IDoorOperation
    {
        public override void ForceResetMemoryState()
        {
        }

        public ValueTask<DoorCommand> ReadDoorCommandAsync(IFlowContext context)
        {
            Console.Write("Door command (Open/Close/Lock/Unlock, Enter to exit): ");
            var text = Console.ReadLine()?.Trim();
            var doorCommand = DoorRules.ParseCommand(text);
            var doorLockCommand = doorCommand == DoorCommand.Unknown ?
                DoorLockRules.ParseCommand(text) :
                DoorLockCommand.Unknown;

            // IDoorOperation の契約を保ったまま、context にコマンド値を保持
            var doorLock = context.GetRequired<RefEntry<DoorLockCommand>>();
            doorLock.Value = doorLockCommand;

            return new(doorCommand);
        }
    }
}
