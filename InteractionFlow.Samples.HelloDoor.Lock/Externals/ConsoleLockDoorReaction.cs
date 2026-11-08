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
    public sealed class ConsoleLockDoorReaction : Reaction, IDoorReaction
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

            var doorLock = context.GetRequired<DoorLockState>();

            // IDoorReaction の契約を保ったまま、context からコマンド値を取得
            var lockCommandRef = context.GetRequired<RefEntry<DoorLockCommand>>();

            var lockCommand = lockCommandRef.Value;
            var message = doorLock.GetMessageAndUpdateState(door, command, lockCommand);
            Console.WriteLine(message);

            return null;
        }
    }
}
