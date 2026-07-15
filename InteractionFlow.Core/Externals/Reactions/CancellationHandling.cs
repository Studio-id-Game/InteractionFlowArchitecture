using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Externals.Reactions
{
    /// <summary>
    /// キャンセルを扱う Reaction のデフォルト実装基底クラスです。
    /// </summary>
    /// <param name="dependency">この Reaction が依存するフローノード。</param>
    public abstract class CancellationHandling(params IDependencyNode[] dependency) : ExceptionHandling<OperationCanceledException>(dependency), ICancellationPort
    {
        /// <summary>
        /// キャンセル前処理、コンテキストのキャンセル待機とリセット、キャンセル後処理を順に実行します。
        /// </summary>
        /// <param name="context">キャンセルが発生した時点のフローコンテキスト。</param>
        /// <param name="exception">処理するキャンセル例外。</param>
        /// <returns>キャンセル処理後のフロー終了結果。</returns>
        public ValueTask<ReactionEnd> HandleCancellationAsync(IFlowContext context, OperationCanceledException exception)
        {
            var _BeforeCancellationCoreAsync = BeforeCancellationCoreAsync(context, exception);
            if (!_BeforeCancellationCoreAsync.IsCompletedSuccessfully)
            {
                return SlowPathAsync(this, _BeforeCancellationCoreAsync, context, exception);

                static async ValueTask<ReactionEnd> SlowPathAsync(CancellationHandling @this, ValueTask before, IFlowContext context, OperationCanceledException exception)
                {
                    await before.ConfigureAwait(false);
                    await context.Cancellation.TryWaitAndResetAsync().ConfigureAwait(false);
                    return await @this.AfterCancellationCoreAsync(context, exception).ConfigureAwait(false);
                }
            }

            var _TryWaitAndResetAsync = context.Cancellation.TryWaitAndResetAsync();
            if (!_TryWaitAndResetAsync.IsCompletedSuccessfully)
            {
                return SlowPathAsync(this, _TryWaitAndResetAsync, context, exception);

                static async ValueTask<ReactionEnd> SlowPathAsync(CancellationHandling @this, ValueTask<bool> before, IFlowContext context, OperationCanceledException exception)
                {
                    await before.ConfigureAwait(false);
                    return await @this.AfterCancellationCoreAsync(context, exception).ConfigureAwait(false);
                }
            }

            return AfterCancellationCoreAsync(context, exception);
        }

        /// <summary>
        /// 例外ハンドリング経由のキャンセル処理を、キャンセル専用の処理へ委譲します。
        /// </summary>
        /// <param name="context">キャンセルが発生した時点のフローコンテキスト。</param>
        /// <param name="exception">処理するキャンセル例外。</param>
        /// <returns>キャンセル処理後のフロー終了結果。</returns>
        protected sealed override ValueTask<ReactionEnd> HandleExceptionCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            return HandleCancellationAsync(context, exception);
        }

        /// <summary>
        /// コンテキストのキャンセル待機とリセットを行う前に実行する派生クラス用の前処理です。
        /// </summary>
        /// <param name="context">キャンセルが発生した時点のフローコンテキスト。</param>
        /// <param name="exception">処理するキャンセル例外。</param>
        /// <returns>前処理の完了を表す値。</returns>
        protected virtual ValueTask BeforeCancellationCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            return default;
        }

        /// <summary>
        /// コンテキストのキャンセル待機とリセット後に、フロー終了結果を生成します。
        /// </summary>
        /// <param name="context">キャンセルが発生した時点のフローコンテキスト。</param>
        /// <param name="exception">処理するキャンセル例外。</param>
        /// <returns>キャンセル処理後のフロー終了結果。</returns>
        protected abstract ValueTask<ReactionEnd> AfterCancellationCoreAsync(IFlowContext context, OperationCanceledException exception);
    }
}
