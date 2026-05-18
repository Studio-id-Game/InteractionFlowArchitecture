using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;

namespace InteractionFlow.Standard.ReactionPorts
{
    public interface IConsoleReaction : IReactionPort, IHasFunctionState<ConsoleState>
    {
    }
}
