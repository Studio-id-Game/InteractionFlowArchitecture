namespace InteractionFlow.Samples.HelloDoor.Lock.Entities
{
    public static class DoorLockRules
    {
        public static DoorLockCommand ParseCommand(string? text)
        {
            return text?.ToUpperInvariant() switch
            {
                "LOCK" => DoorLockCommand.Lock,
                "UNLOCK" => DoorLockCommand.Unlock,
                _ => DoorLockCommand.Unknown,
            };
        }
    }
}
