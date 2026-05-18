using InteractionFlow.Standard.Entities.Consoles;

namespace InteractionFlow.Standard.SilentExternalPorts
{
    public interface IConsoleCursorPositionAccess : ISilentRequestPort<ConsoleCursorPosition, ConsoleCursorPosition>
    {
        public ConsoleCursorPosition Position { get; set; }
    }
}
