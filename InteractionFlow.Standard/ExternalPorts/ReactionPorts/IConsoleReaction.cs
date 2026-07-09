using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;

namespace InteractionFlow.Standard.ExternalPorts.ReactionPorts
{
    /// <summary>
    /// コンソール出力状態を持つ Reaction ポートを表します。
    /// </summary>
    public interface IConsoleReaction : IReactionPort, IHasFunctionState<ConsoleState>
    {
    }
}
