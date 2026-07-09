using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.SilentExternals;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.SilentExternals
{
    /// <summary>
    /// Console.CancelKeyPress をフローコンテキストのキャンセル制御へ接続する標準 SilentExternal 実装です。
    /// </summary>
    public class CancellationWithConsole : SilentExternal, ICancellationWithConsole
    {
        ConsoleCancelEventHandler? cancelKeyPress;

        /// <summary>
        /// 指定されたコンテキストに対して Ctrl+C キャンセル連携を設定します。
        /// </summary>
        /// <param name="context">キャンセル連携を設定するフローコンテキスト。</param>
        /// <returns>設定処理の完了を表す値。</returns>
        public ValueTask Setup(IFlowContext context)
        {
            if (cancelKeyPress != null)
            {
                Console.CancelKeyPress -= cancelKeyPress;
            }

            cancelKeyPress = (sender, args) =>
            {
                CancelKeyPress(context, args);
            };

            Console.CancelKeyPress += cancelKeyPress;

            return default;
        }

        /// <summary>
        /// 登録済みの Console.CancelKeyPress ハンドラを解除します。
        /// </summary>
        public override void ForceResetMemoryState()
        {
            if (cancelKeyPress != null)
            {
                Console.CancelKeyPress -= cancelKeyPress;
            }
        }

        /// <summary>
        /// Ctrl+C 入力を受け取り、対象コンテキストに監視対象タスクがある場合はキャンセルを要求します。
        /// </summary>
        /// <param name="context">キャンセル要求を伝えるフローコンテキスト。</param>
        /// <param name="args">コンソールキャンセルイベント引数。</param>
        protected virtual void CancelKeyPress(IFlowContext context, ConsoleCancelEventArgs args)
        {
            if (context.Cancellation.HasTask)
            {
                context.Cancellation.Cancel();
            }
            args.Cancel = true;
        }
    }
}
