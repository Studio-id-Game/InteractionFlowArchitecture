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

            if (!context.TryGet<DoorLockState>(out var doorLock))
            {
                return new Exception("No door lock context.");
            }

            // IDoorReaction の契約を保ったまま、context からコマンド値を取得
            if (!context.TryGet<RefEntry<DoorLockCommand>>(out var lockCommandRef))
            {
                return new Exception("No door lock command context.");
            }

            var lockCommand = lockCommandRef.Value;
            var message = doorLock.GetMessageAndUpdateState(door, command, lockCommand);
            Console.WriteLine(message);

            return null;
        }
    }
}
