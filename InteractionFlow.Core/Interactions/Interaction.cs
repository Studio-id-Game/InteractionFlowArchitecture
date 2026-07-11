using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Interactions
{
    /// <summary>
    /// <see cref="IInteraction"/> のデフォルト実装基底クラスです。
    /// </summary>
    /// <param name="exceptionPort">通常の例外をフロー終了時の反応へ変換するポート。</param>
    /// <param name="cancellationPort">キャンセルをフロー終了時の反応へ変換するポート。</param>
    /// <param name="dependency">この Interaction が明示的に依存するフローノード。</param>
    public abstract class Interaction(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        params IFlowNode[] dependency)
        : IInteraction
    {
        /// <summary>
        /// 例外処理ポート、キャンセル処理ポート、および派生クラスから渡された依存ノードを取得します。
        /// </summary>
        public ReadOnlySpan<IFlowNode> Dependency => (IFlowNode[])[ExceptionPort, CancellationPort, .. dependency];

        /// <summary>
        /// 通常の例外を処理する Reaction ポートを取得します。
        /// </summary>
        protected IExceptionPort<Exception> ExceptionPort => exceptionPort;

        /// <summary>
        /// キャンセルを処理する Reaction ポートを取得します。
        /// </summary>
        protected ICancellationPort CancellationPort => cancellationPort;

        /// <summary>
        /// 指定されたコンテキストで Interaction を実行します。
        /// </summary>
        /// <param name="context">Interaction に渡すフローコンテキスト。</param>
        /// <returns>Interaction の終了結果。</returns>
        public async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            ReactionEnd end;
            try
            {
                if (context.Cancellation.TryGetCanceledException(out var e))
                {
                    throw e!;
                }

                var task = ExecuteCoreAsync(context);

                context.Cancellation.AddCancelableTask(CancelableTask());

                async Task CancelableTask()
                {
                    try
                    {
                        await task.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        await OnCancellation(context).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // ここでの例外は、下の return await task; でハンドリングする事を前提として、握りつぶす。
                        // これにより、CancelableTask はキャンセル時の追加処理だけを担当できる。
                    }
                }

                end = await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException e)
            {
                e = new OperationCanceledException($"{this.GetName()} Interaction was canceled.", e);
                end = await HandleCancellationAsync(context, e).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                end = await HandleExceptionAsync(context, e).ConfigureAwait(false);
            }

            return GetEnd(context, end);
        }

        /// <summary>
        /// 指定されたコンテキストで Interaction の本体を実行します。
        /// </summary>
        /// <param name="context">Interaction に渡すフローコンテキスト。</param>
        /// <returns>Reaction が生成した終了結果。</returns>
        protected abstract Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context);

        /// <summary>
        /// キャンセル時に、Interaction 固有の追加処理を実行します。
        /// </summary>
        /// <param name="context">現在のフローコンテキスト。</param>
        /// <returns>追加処理の完了を表すタスク。</returns>
        protected virtual Task OnCancellation(IFlowContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 指定されたキャンセル例外をキャンセル処理ポートへ委譲します。
        /// </summary>
        /// <param name="context">キャンセルが発生した時点のフローコンテキスト。</param>
        /// <param name="e">処理するキャンセル例外。</param>
        /// <returns>キャンセル処理後のフロー終了結果。</returns>
        private async Task<ReactionEnd> HandleCancellationAsync(IFlowContext context, OperationCanceledException e)
        {
            return await CancellationPort.HandleCancellationAsync(context, e).ConfigureAwait(false);
        }

        /// <summary>
        /// 指定された例外を例外処理ポートへ委譲します。
        /// </summary>
        /// <param name="context">例外が発生した時点のフローコンテキスト。</param>
        /// <param name="e">処理する例外。</param>
        /// <returns>例外処理後のフロー終了結果。</returns>
        private async Task<ReactionEnd> HandleExceptionAsync(IFlowContext context, Exception e)
        {
            return await ExceptionPort.HandleExceptionAsync(context, e).ConfigureAwait(false);
        }

        /// <summary>
        /// Reaction が生成した終了結果を、Interaction に渡されたコンテキストへ結合します。
        /// </summary>
        /// <param name="context">Interaction に渡されたフローコンテキスト。</param>
        /// <param name="reactionEnd">Reaction が生成したフロー終了結果。</param>
        /// <returns>Interaction の終了トークン。</returns>
        private static FlowEndToken GetEnd(IFlowContext context, ReactionEnd reactionEnd)
        {
            return IInteraction.GetEnd(context, reactionEnd);
        }
    }
}
