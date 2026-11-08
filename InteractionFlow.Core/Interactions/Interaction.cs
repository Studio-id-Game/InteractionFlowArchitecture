using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using System;
using System.Collections.Generic;
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
                if (Cancellation is T t)
                {
                    value = t;
                    return true;
                }
                else
                {
                    return parent.TryGet(out value);
                }
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
        /// <remarks>
        /// 渡されたコンテキストの所有権は取得せず、破棄しません。
        /// 本体で発生した例外とキャンセルは対応する Reaction ポートへ委譲しますが、
        /// そのポート自身が例外を送出した場合は終了トークンを生成せず、その例外を呼び出し側へ伝播します。
        /// </remarks>
        /// <exception cref="Exception">例外処理またはキャンセル処理を担当するポートから未処理の例外が伝播した場合。</exception>
        public async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            IDisposable? registration = null;
            TaskCompletionSource<bool>? completion = null;
            Exception? cleanupFailure = null;
            ReactionEnd end;
            try
            {
                if (context.Cancellation.TryGetCanceledException(out var e)) throw e!;

                completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                // Reject registration during reset before starting any untracked work.
                registration = context.Cancellation.AddCancelableTask(completion.Task);
                end = await ExecuteBodyAndSignalCompletionAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException e)
            {
                e = new OperationCanceledException($"{this.GetName()} Interaction was canceled.", e);
                end = await HandleCancellationAsync(context, e, cleanupFailure, registration).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                end = await HandleExceptionAsync(context, e).ConfigureAwait(false);
            }
            finally
            {
                // A port may throw or skip reset; still observe the private notification's failure.
                if (cleanupFailure != null) _ = completion!.Task.Exception;
                registration?.Dispose();
            }
            return GetEnd(end);

            async Task<ReactionEnd> ExecuteBodyAndSignalCompletionAsync()
            {
                try
                {
                    // Synchronous startup errors bypass OnCancellation, preserving its existing contract.
                    var task = ExecuteCoreAsync(context);
                    return await AwaitBodyAndCleanupAsync(task).ConfigureAwait(false);
                }
                finally
                {
                    // Complete before either port can wait on this execution's notification.
                    completion!.TrySetResult(true);
                }
            }

            async Task<ReactionEnd> AwaitBodyAndCleanupAsync(Task<ReactionEnd> task)
            {
                try { return await task.ConfigureAwait(false); }
                catch (OperationCanceledException)
                {
                    try { await OnCancellation(context).ConfigureAwait(false); }
                    catch (Exception error)
                    {
                        cleanupFailure = error;
                        completion!.TrySetException(error);
                    }
                    throw;
                }
            }
        }

        /// <summary>
        /// 指定された Interaction を、現在のコンテキスト値を参照できる独立したキャンセルスコープで実行します。
        /// </summary>
        /// <param name="nestedInteraction">ネストして実行する Interaction。</param>
        /// <param name="context">親 Interaction のフローコンテキスト。</param>
        /// <returns>ネストした Interaction の Reaction が生成した終了結果。</returns>
        /// <exception cref="Exception">ネストした Interaction が未解決の例外で終了した場合にスローされます。</exception>
        [Obsolete("Compose Interactions in a SystemFlow instead. NestedExecuteAsync is a legacy API; " +
            "direct nested ExecuteAsync calls with a shared CancellationObject can deadlock.")]
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
        [Obsolete("Compose Interactions in a SystemFlow instead. NestedExecuteAsync is a legacy API; " +
            "direct nested ExecuteAsync calls with a shared CancellationObject can deadlock.")]
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
        /// 本体 Task がキャンセル例外で終了したとき、CancellationPort に委譲する前に追加処理を実行します。
        /// </summary>
        /// <param name="context">現在のフローコンテキスト。</param>
        /// <returns>追加処理の完了を表すタスク。</returns>
        /// <remarks>
        /// 開始前キャンセルや、本体が Task を返す前の同期例外では呼びません。
        /// 失敗はキャンセル完了の待機 Task へ保持し、元のキャンセルと共に扱います。
        /// この処理内で同じ CancellationObject の WaitForTasksAndResetCancellationAsync を待ってはいけません。
        /// </remarks>
        protected virtual Task OnCancellation(IFlowContext context)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 指定されたキャンセル例外をキャンセル処理ポートへ委譲します。
        /// </summary>
        /// <param name="context">キャンセルが発生した時点のフローコンテキスト。</param>
        /// <param name="cancellation">処理するキャンセル例外。</param>
        /// <param name="cleanupFailure">保持が必要な後始末の失敗。</param>
        /// <param name="registration">後始末の失敗を待機側へ渡す登録。</param>
        /// <returns>キャンセル処理後のフロー終了結果。</returns>
        private async Task<ReactionEnd> HandleCancellationAsync(
            IFlowContext context, OperationCanceledException cancellation,
            Exception? cleanupFailure, IDisposable? registration)
        {
            ReactionEnd end;
            try
            {
                end = await CancellationPort.HandleCancellationAsync(context, cancellation).ConfigureAwait(false);
            }
            catch (Exception portFailure) when (cleanupFailure != null)
            {
                if (ContainsException(portFailure, cleanupFailure)
                    && ContainsException(portFailure, cancellation.InnerException ?? cancellation)) throw;
                throw PreserveCancellationFailures(cancellation, cleanupFailure, portFailure);
            }

            // Once failure was delivered through reset, respect the port's explicit recovery or translation.
            var collected = registration != null && context.Cancellation.HasCollectedTaskFailure(registration);
            if (cleanupFailure != null && !collected && (!ContainsException(end.Exception, cleanupFailure)
                || !ContainsException(end.Exception, cancellation.InnerException ?? cancellation)))
                throw PreserveCancellationFailures(cancellation, cleanupFailure, end.Exception);
            return end;
        }

        private static Exception PreserveCancellationFailures(
            OperationCanceledException cancellation, Exception cleanupFailure, Exception? portFailure)
        {
            var failures = new List<Exception>();
            if (portFailure != null) failures.Add(portFailure);
            if (!ContainsException(portFailure, cancellation.InnerException ?? cancellation))
                failures.Add(cancellation);
            if (!failures.Exists(error => ContainsException(error, cleanupFailure))) failures.Add(cleanupFailure);
            return new AggregateException(failures);
        }

        private static bool ContainsException(Exception? error, Exception expected)
        {
            if (ReferenceEquals(error, expected)) return true;
            if (error is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                    if (ContainsException(inner, expected)) return true;
                return false;
            }
            return error?.InnerException != null && ContainsException(error.InnerException, expected);
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
        /// Reaction が生成した終了結果から Interaction の終了トークンを生成します。
        /// </summary>
        /// <param name="reactionEnd">Reaction が生成したフロー終了結果。</param>
        /// <returns>Interaction の終了トークン。</returns>
        private static FlowEndToken GetEnd(ReactionEnd reactionEnd)
        {
            return IInteraction.GetEnd(reactionEnd);
        }
    }
}
