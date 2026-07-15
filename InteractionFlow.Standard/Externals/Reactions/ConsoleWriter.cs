using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Reactions;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.Externals.ConsoleRule;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Reactions
{
    /// <summary>
    /// コンソールへ文字列を出力する標準 Reaction 実装です。
    /// </summary>
    public class ConsoleWriter : Reaction, IConsoleWriter
    {
        /// <summary>
        /// 既定のコンソール出力状態でインスタンスを作成します。
        /// </summary>
        public ConsoleWriter() : base()
        {
            if (State == null)
                throw new ArgumentNullException("state");
        }

        /// <summary>
        /// コンソール出力に使用する状態を取得または設定します。
        /// </summary>
        public ConsoleState State { get; set; }

        /// <summary>
        /// コンソール出力状態を既定値へ戻します。
        /// </summary>
        public override void ForceResetMemoryState()
        {
            State = ConsoleState.Default;
        }

        /// <summary>
        /// 現在の状態に従ってコンソールへ文字列を書き込みます。
        /// </summary>
        /// <param name="context">出力時点のフローコンテキスト。</param>
        /// <param name="consoleOutput">出力する文字列。</param>
        /// <returns>出力後のフロー終了結果。</returns>
        public ValueTask<ReactionEnd> Write(IFlowContext context, ConsoleOutput consoleOutput)
        {
            using var cc = new ConsoleColorScope();
            cc.State = State.ColorSet;

            if (State.WriteLine)
            {
                Console.WriteLine(consoleOutput.text);
            }
            else
            {
                Console.Write(consoleOutput.text);
            }

            return new(GetEnd());
        }
    }
}
