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
    /// キャンセル開始と完了をコンソールへ出力する標準 Reaction 実装です。
    /// </summary>
    public class ConsoleCancellationHandling : CancellationHandling, IConsoleReaction
    {
        /// <summary>
        /// 既定のキャンセル表示状態でインスタンスを作成します。
        /// </summary>
        public ConsoleCancellationHandling() : base()
        {
            if (State == null)
                throw new ArgumentNullException("state");
        }

        /// <summary>
        /// キャンセル表示に使用するコンソール状態を取得または設定します。
        /// </summary>
        public ConsoleState State { get; set; }

        /// <summary>
        /// キャンセル表示状態を既定値へ戻します。
        /// </summary>
        public override void ForceResetMemoryState()
        {
            ThrowException = false;
            State = ConsoleState.Default;
            State.Update(foregroundColor: ConsoleColor.Yellow);
        }

        /// <summary>
        /// キャンセル待機とリセットの前に、キャンセル開始メッセージを出力します。
        /// </summary>
        /// <param name="context">キャンセルが発生した時点のフローコンテキスト。</param>
        /// <param name="exception">処理するキャンセル例外。</param>
        /// <returns>前処理の完了を表す値。</returns>
        protected override ValueTask BeforeCancellationCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            using (var cc = new ConsoleColorScope())
            {
                cc.State = State.ColorSet;
                if (State.WriteLine)
                {
                    Console.WriteLine();
                }

                Console.Write($"* Cancel... : {exception.Message} ");
            }

            if (State.WriteLine)
            {
                Console.WriteLine();
            }

            return default;
        }

        /// <summary>
        /// キャンセル待機とリセットの後に、キャンセル完了メッセージを出力します。
        /// </summary>
        /// <param name="context">キャンセルが発生した時点のフローコンテキスト。</param>
        /// <param name="exception">処理するキャンセル例外。</param>
        /// <returns>キャンセル表示後のフロー終了結果。</returns>
        protected override ValueTask<ReactionEnd> AfterCancellationCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            using (var cc = new ConsoleColorScope())
            {
                cc.State = State.ColorSet;
                if (State.WriteLine)
                {
                    Console.WriteLine();
                }

                Console.Write($"> Cancel Completed.");
            }

            if (State.WriteLine)
            {
                Console.WriteLine();
            }

            return new(GetEnd(exception));
        }
    }
}
