using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.Console.Entities;

namespace InteractionFlow.Standard.Console.ExternalPorts.ReactionPorts
{
    /// <summary>
    /// コンソール出力状態を持つ Reaction ポートを表します。
    /// </summary>
    public interface IConsoleReaction : IReactionPort, IHasFunctionState<ConsoleState>
    {
    }
}
