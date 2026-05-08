using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Standard.Entities.Consoles;

namespace InteractionFlow.Standard.ReactionPorts
{
    public interface IConsoleReaction : IReactionPort
    {
        public ConsoleState State { get; set; }
    }
}
