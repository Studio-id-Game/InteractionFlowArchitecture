namespace InteractionFlow.Samples.HelloDoor.Entities
{
    public static class DoorRules
    {
        public static DoorCommand ParseCommand(string? text)
        {
            return text?.ToUpperInvariant() switch
            {
                "OPEN" => DoorCommand.Open,
                "CLOSE" => DoorCommand.Close,
                "" or null => DoorCommand.Exit,
                _ => DoorCommand.Unknown,
            };
        }
    }
}
