using InteractionFlow.Samples.HelloDoor.Entities;

namespace InteractionFlow.Samples.HelloDoor.Lock.Entities
{

    public sealed class DoorLockState
    {
        public bool IsLocked { get; set; } = true;

        public string GetMessageAndUpdateState(DoorState door, DoorCommand command, DoorLockCommand lockCommand)
        {
            if (IsLocked)
            {
                switch (lockCommand)
                {
                    case DoorLockCommand.Lock:
                        return "The door is already locked.";

                    case DoorLockCommand.Unlock:
                        IsLocked = false;
                        return "The door unlocked.";
                }
            }
            else
            {
                switch (lockCommand)
                {
                    case DoorLockCommand.Lock:
                        IsLocked = true;
                        return door.IsOpen ?
                            "The door locked... but it's still open." :
                            "The door locked.";

                    case DoorLockCommand.Unlock:
                        return "The door is already unlocked.";
                }
            }

            if (command == DoorCommand.Open && !door.IsOpen && IsLocked)
            {
                return "The door is locked!";
            }
            else if (command != DoorCommand.Unknown)
            {
                return door.ApplyCommand(command);
            }

            return "Use Open, Close, Lock or Unlock.";
        }
    }
}
