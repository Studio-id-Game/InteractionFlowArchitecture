using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;

namespace InteractionFlow.Standard.ExternalPorts.ReactionPorts
{
    public interface IConsoleReaction : IReactionPort, IHasFunctionState<ConsoleState>
    {
    }
}
