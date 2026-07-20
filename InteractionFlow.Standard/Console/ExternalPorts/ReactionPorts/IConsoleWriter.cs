using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.Console.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Console.ExternalPorts.ReactionPorts
{
    /// <summary>
    /// コンソールへ文字列を出力する Reaction ポートを表します。
    /// </summary>
    public interface IConsoleWriter : IConsoleReaction
    {
        /// <summary>
        /// 指定された出力をコンソールへ書き込みます。
        /// </summary>
        /// <param name="context">出力時点のフローコンテキスト。</param>
        /// <param name="consoleOutput">出力する文字列。</param>
        /// <returns>出力後のフロー終了結果。</returns>
        public ValueTask<ReactionEnd> Write(IFlowContext context, ConsoleOutput consoleOutput);
    }
}
