using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Standard.Old.Console.Entities;

namespace InteractionFlow.Standard.Old.Console.ExternalPorts.ReactionPorts
{
    /// <summary>
    /// コンソール出力状態を持つ Reaction ポートを表します。
    /// </summary>
    public interface IConsoleReaction : IReactionPort, IHasFunctionState<ConsoleState>
    {
    }
}
