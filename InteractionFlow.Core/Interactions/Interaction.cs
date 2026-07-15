using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using System;
using System.Diagnostics.CodeAnalysis;
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
        params IDependencyNode[] dependency)
        : IInteraction
    {
        private sealed class NestedFlowContext(IFlowContext parent) : IFlowContext
        {
            public CancellationObject Cancellation { get; } = new();

            public bool TryGet<T>([MaybeNullWhen(false)] out T value)
            {
                return parent.TryGet(out value);
            }
        }

        private readonly IDependencyNode[] dependencies = [exceptionPort, cancellationPort, .. dependency];

        /// <summary>
        /// 例外処理ポート、キャンセル処理ポート、および派生クラスから渡された依存ノードを取得します。
        /// </summary>
        public ReadOnlyMemory<IDependencyNode> Dependency => dependencies;

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
                end = await task.ConfigureAwait(false);

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
        /// 指定された Interaction を、現在のコンテキスト値を参照できる独立したキャンセルスコープで実行します。
        /// </summary>
        /// <param name="nestedInteraction">ネストして実行する Interaction。</param>
        /// <param name="context">親 Interaction のフローコンテキスト。</param>
        /// <returns>ネストした Interaction の Reaction が生成した終了結果。</returns>
        /// <exception cref="Exception">ネストした Interaction が未解決の例外で終了した場合にスローされます。</exception>
        protected static async Task<ReactionEnd> NestedExecuteAsync(IInteraction nestedInteraction, IFlowContext context)
        {
            var nestedContext = new NestedFlowContext(context);
            using var registration = context.Cancellation.GetToken().Register(nestedContext.Cancellation.Cancel);

            var end = await nestedInteraction.ExecuteAsync(nestedContext);

            if (end.HasException)
            {
                throw end.Exception!;
            }
            else
            {
                return end.End;
            }
        }

        /// <summary>
        /// 指定された関数を、現在のコンテキスト値を参照できる独立したキャンセルスコープで実行します。
        /// </summary>
        /// <param name="nestedInteractionExecuteAsync">ネストしたキャンセルスコープのフローコンテキストを受け取り、Reaction の終了結果を返す関数。</param>
        /// <param name="context">親 Interaction のフローコンテキスト。</param>
        /// <returns>指定された関数が返した Reaction の終了結果。</returns>
        /// <exception cref="Exception">指定された関数が未解決の例外を含む終了結果を返した場合にスローされます。</exception>
        protected static async Task<ReactionEnd> NestedExecuteAsync(Func<IFlowContext, Task<ReactionEnd>> nestedInteractionExecuteAsync, IFlowContext context)
        {
            var nestedContext = new NestedFlowContext(context);
            using var registration = context.Cancellation.GetToken().Register(nestedContext.Cancellation.Cancel);

            var end = await nestedInteractionExecuteAsync(nestedContext);

            if (end.HasException)
            {
                throw end.Exception!;
            }
            else
            {
                return end;
            }
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
