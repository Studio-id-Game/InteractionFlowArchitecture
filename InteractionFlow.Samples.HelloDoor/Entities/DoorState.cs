namespace InteractionFlow.Samples.HelloDoor.Entities
{
    public sealed class DoorState
    {
        public bool IsOpen { get; set; }

        public bool ExitRequested { get; set; }

        public string ApplyCommand(DoorCommand command)
        {
            if (IsOpen)
            {
                switch (command)
                {
                    case DoorCommand.Open:
                        return "The door is already open.";

                    case DoorCommand.Close:
                        IsOpen = false;
                        return "The door closed.";

                    case DoorCommand.Exit:
                        ExitRequested = true;
                        return "Welcome to the world of Interaction Flow! Enjoy your adventure!";
                }
            }
            else
            {
                switch (command)
                {
                    case DoorCommand.Open:
                        IsOpen = true;
                        return "The door opened.";

                    case DoorCommand.Close:
                        return "The door is already closed.";

                    case DoorCommand.Exit:
                        return "You need to open the door to exit!";
                }
            }

            return "Use Open or Close.";
        }
    }
}
