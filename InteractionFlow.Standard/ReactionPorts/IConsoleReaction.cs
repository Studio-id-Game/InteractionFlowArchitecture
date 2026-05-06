using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Standard.Entities.Consoles;

namespace InteractionFlow.Standard.ReactionPorts
{
    public interface IConsoleReaction : IReactionPort<ConsoleOutput>, IReactionPort<ConsolePositionAccess>
    {
        public ConsoleState State { get; set; }
        public ConsoleState ErrorState { get; set; }
        public ConsoleState CancelState { get; set; }
    }
}
