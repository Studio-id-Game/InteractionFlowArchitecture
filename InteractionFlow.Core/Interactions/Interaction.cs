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
        public abstract Task<FlowEndToken> ExecuteAsync(IFlowContext context);

        /// <summary>
        /// 指定されたキャンセル例外をキャンセル処理ポートへ委譲します。
        /// </summary>
        /// <param name="context">キャンセルが発生した時点のフローコンテキスト。</param>
        /// <param name="e">処理するキャンセル例外。</param>
        /// <returns>キャンセル処理後のフロー終了トークン。</returns>
        protected async Task<FlowEndToken> HandleCancellationAsync(IFlowContext context, OperationCanceledException e)
        {
            return await CancellationPort.HandleCancellationAsync(context, e).ConfigureAwait(false);
        }

        /// <summary>
        /// 指定された例外を例外処理ポートへ委譲します。
        /// </summary>
        /// <param name="context">例外が発生した時点のフローコンテキスト。</param>
        /// <param name="e">処理する例外。</param>
        /// <returns>例外処理後のフロー終了トークン。</returns>
        protected async Task<FlowEndToken> HandleExceptionAsync(IFlowContext context, Exception e)
        {
            return await ExceptionPort.HandleExceptionAsync(context, e).ConfigureAwait(false);
        }

        /// <summary>
        /// Interaction の実行本体を、ライブラリ標準の例外ハンドリング内で実行します。
        /// <para>
        /// <see cref="OperationCanceledException"/> およびその他の <see cref="Exception"/> は捕捉され、
        /// <see cref="FlowEndToken"/> に変換されます。
        /// </para>
        /// <para>
        /// <paramref name="attachCancellation"/> が指定された場合、
        /// Task の cancellation 監視を追加します。
        /// この監視は cancellation 同期のみを目的としており、
        /// 例外処理は本メソッドの main await path 側で行われます。
        /// </para>
        /// </summary>
        /// <param name="context">
        /// Interaction の実行コンテキスト。
        /// </param>
        /// <param name="function">
        /// 実行する Interaction 本体。
        /// </param>
        /// <param name="attachCancellation">
        /// 非同期 cancellation 時に実行される追加処理。
        /// </param>
        /// <returns>
        /// Railway 変換後の <see cref="FlowEndToken"/> を返します。
        /// </returns>
        protected async Task<FlowEndToken> TryCatchBlockAsync(IFlowContext context, Func<IFlowContext, Task<FlowEndToken>> function, Func<ValueTask>? attachCancellation = null)
        {
            try
            {
                if (context.Cancellation.TryGetCanceledException(out var e))
                {
                    throw e!;
                }

                var task = function(context);

                if (attachCancellation != null)
                {
                    context.Cancellation.AddCancelableTask(CancelableTask(task, attachCancellation));

                    static async Task CancelableTask(Task task, Func<ValueTask>? attachCancellation)
                    {
                        try
                        {
                            await task.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            if (attachCancellation != null)
                            {
                                await attachCancellation().ConfigureAwait(false);
                            }
                        }
                        catch (Exception)
                        {
                            // ここでの例外は、下の return await task; でハンドリングする事を前提として、握りつぶす。
                            // これにより、ObserveLifetime は純粋な寿命監視の目的を果たす。 
                        }
                    }
                }

                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException e)
            {
                e = new OperationCanceledException($"{this.GetName()} Interaction was canceled.", e);
                var end = await HandleCancellationAsync(context, e).ConfigureAwait(false);
                end.Exception = e;
                return end;
            }
            catch (Exception e)
            {
                var end = await HandleExceptionAsync(context, e).ConfigureAwait(false);
                end.Exception = e;
                return end;
            }
        }

    }
}
