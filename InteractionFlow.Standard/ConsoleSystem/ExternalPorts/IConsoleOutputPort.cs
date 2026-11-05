using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ConsoleSystem.ExternalPorts
{
    /// <summary>標準出力へ文字列を表示します。</summary>
    public interface IConsoleOutputPort : IReactionPort
    {
        /// <summary>改行せずに表示します。適用した色は出力後に復元します。</summary>
        ValueTask<ReactionEnd> WriteAsync(IFlowContext context, string text, ConsoleTextStyle style = default);
        /// <summary>改行を付けて表示します。適用した色は出力後に復元します。</summary>
        ValueTask<ReactionEnd> WriteLineAsync(IFlowContext context, string text, ConsoleTextStyle style = default);
    }
}
