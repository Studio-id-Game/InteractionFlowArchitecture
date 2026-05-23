using InteractionFlow.Standard.Entities.Consoles;

namespace InteractionFlow.Standard.ExternalPorts.SilentPorts
{
    public interface IConsoleCursorPositionAccess : ISilentRequestPort<ConsoleCursorPosition, ConsoleCursorPosition>
    {
        public ConsoleCursorPosition Position { get; set; }
    }
}
