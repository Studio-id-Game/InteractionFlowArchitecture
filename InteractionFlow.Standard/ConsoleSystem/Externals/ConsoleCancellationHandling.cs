using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ConsoleSystem.Externals
{
    /// <summary>Core の待機・リセット後にキャンセル通知を標準エラーへ表示します。</summary>
    public sealed class ConsoleCancellationHandling : CancellationHandling
    {
        /// <inheritdoc/>
        public override void ForceResetMemoryState() => ThrowException = false;

        /// <inheritdoc/>
        protected override ValueTask<ReactionEnd> AfterCancellationCoreAsync(
            IFlowContext context, OperationCanceledException exception, Result waitAndResetResult)
        {
            waitAndResetResult.Try(out var waitFailure);
            var writeFailure = ConsoleOutputWriter.WriteErrorLine("Operation canceled.");
            var failure = ConsoleOutputWriter.Combine(waitFailure, writeFailure);
            // 通知・リセットの失敗時は元のキャンセルも失わない。
            return new(GetEnd(failure is null ? null : ConsoleOutputWriter.Combine(exception, failure)));
        }
    }
}
