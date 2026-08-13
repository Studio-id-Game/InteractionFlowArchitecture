using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Reactions;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.ReactionPorts;
using InteractionFlow.Samples.HelloDoor.Lock.Entities;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.Lock.Externals.Reactions
{
    public sealed class ConsoleLockDoorReaction : Reaction, IDoorReaction
    {
        public override void ForceResetMemoryState()
        {
        }

        public ValueTask<ReactionEnd> ReactAsync(IFlowContext context, DoorCommand command)
        {
            if (!context.TryGet<DoorState>(out var door))
            {
                return new(GetEnd(new Exception("No door context.")));
            }

            if (!context.TryGet<DoorLockState>(out var doorLock))
            {
                return new(GetEnd(new Exception("No door lock context.")));
            }

            if (!context.TryGet<RefEntry<DoorLockCommand>>(out var lockCommandRef))
            {
                return new(GetEnd(new Exception("No door lock command context.")));
            }

            var lockCommand = lockCommandRef.Value;
            lockCommandRef.Value = DoorLockCommand.Unknown;

            Console.WriteLine(GetMessageAndUpdateState(door, doorLock, command, lockCommand));
            return new(GetEnd());
        }

        private static string GetMessageAndUpdateState(DoorState door, DoorLockState doorLock, DoorCommand command, DoorLockCommand lockCommand)
        {
            switch (command)
            {
                case DoorCommand.Open when !door.IsOpen && !doorLock.IsLocked:
                    door.IsOpen = true;
                    return "The door opens.";

                case DoorCommand.Open when !door.IsOpen && doorLock.IsLocked:
                    return "The door is locked.";

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
            }

            switch (lockCommand)
            {
                case DoorLockCommand.Lock when !doorLock.IsLocked:
                    doorLock.IsLocked = true;
                    return "The door locks.";

                case DoorLockCommand.Lock:
                    return "The door is already locked.";

                case DoorLockCommand.Unlock when doorLock.IsLocked:
                    doorLock.IsLocked = false;
                    return "The door unlocks.";

                case DoorLockCommand.Unlock:
                    return "The door is already unlocked.";
            }


            return "Use Open, Close, Lock or Unlock.";
        }
    }
}
