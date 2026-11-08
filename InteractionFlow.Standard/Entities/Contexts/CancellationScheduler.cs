using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Entities.Contexts
{
    /// <summary>期限到達時にキャンセルを通知し、対象 Task の終了結果と通知失敗をまとめます。</summary>
    /// <remarks>
    /// 同時に一件の時間制限を扱います。完了後は再利用できます。
    /// CancellationObject の待機・リセットや期間分離は行いません。
    /// </remarks>
    public sealed class CancellationScheduler : IDisposable
    {
        private sealed class TimeoutOperation
        {
            public CancellationTokenSource TimerCancellation { get; } = new();
            public bool Notifying { get; set; }
            public int NotificationThreadId { get; set; }
        }

        private readonly object gate = new();
        private readonly CancellationObject cancellation;
        private TimeoutOperation? current;
        private bool disposed;

        internal CancellationScheduler(CancellationObject cancellation) => this.cancellation = cancellation;

        /// <summary>Task が期限までに完了しなければ Cancel を呼び、通知後も Task の終了を待ちます。</summary>
        /// <param name="timeoutTime">ゼロ以上・Int32.MaxValue ミリ秒以下、または Timeout.InfiniteTimeSpan。</param>
        /// <param name="task">開始済みの処理。Scheduler は処理の開始や別スレッドへの移動を行いません。</param>
        /// <returns>処理と Cancel 通知の結果。両方の失敗は AggregateException に保持します。</returns>
        /// <remarks>
        /// Task の失敗 Result、送出例外、キャンセル完了を、この API の明示的な境界で Result に変換します。
        /// 処理が正常完了すれば、期限到達自体を失敗にはしません。
        /// 引数・破棄済み・同時利用の誤りは同期的に例外を送出します。
        /// </remarks>
        public Task<Result> TimeoutAsync(TimeSpan timeoutTime, Task<Result> task)
        {
            if (task is null) throw new ArgumentNullException(nameof(task));
            if (timeoutTime != Timeout.InfiniteTimeSpan
                && (timeoutTime < TimeSpan.Zero || timeoutTime.TotalMilliseconds > int.MaxValue))
                throw new ArgumentOutOfRangeException(nameof(timeoutTime));

            TimeoutOperation operation;
            Task delay;
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException(nameof(CancellationScheduler));
                if (current != null) throw new InvalidOperationException("A timeout operation is already active.");
                operation = new TimeoutOperation();
                try { delay = Task.Delay(timeoutTime, operation.TimerCancellation.Token); }
                catch
                {
                    operation.TimerCancellation.Dispose();
                    throw;
                }
                current = operation;
            }
            // Do not hold the scheduler lock while invoking user cancellation callbacks.
            return RunAsync(operation, delay, task);
        }

        private async Task<Result> RunAsync(TimeoutOperation operation, Task delay, Task<Result> task)
        {
            try
            {
                await Task.WhenAny(task, delay).ConfigureAwait(false);
                Exception? notificationFailure = null;
                bool notify;
                lock (gate)
                {
                    // Prefer a task already completed at this decision point, even when the timer also completed.
                    notify = !disposed && !task.IsCompleted && delay.Status == TaskStatus.RanToCompletion;
                    if (notify)
                    {
                        operation.Notifying = true;
                        operation.NotificationThreadId = Thread.CurrentThread.ManagedThreadId;
                    }
                }
                if (notify)
                {
                    try { cancellation.Cancel(); }
                    catch (Exception error) { notificationFailure = error; }
                    finally
                    {
                        lock (gate)
                        {
                            operation.Notifying = false;
                            operation.NotificationThreadId = 0;
                            Monitor.PulseAll(gate);
                        }
                    }
                }

                Result result;
                try { result = await task.ConfigureAwait(false); }
                catch (Exception error)
                {
                    // Await throws one exception; retain all failures of a faulted WhenAll task.
                    result = task.Exception is AggregateException aggregate && aggregate.InnerExceptions.Count > 1
                        ? aggregate : error;
                }
                if (notificationFailure is null) return result;
                return result.Try(out var taskFailure)
                    ? notificationFailure : new AggregateException(notificationFailure, taskFailure);
            }
            finally
            {
                lock (gate)
                {
                    operation.TimerCancellation.Cancel();
                    operation.TimerCancellation.Dispose();
                    current = null;
                }
            }
        }

        /// <summary>期限の予約を解除します。対象 Task や CancellationObject は破棄・キャンセルしません。</summary>
        /// <remarks>
        /// 開始済みの Cancel 通知は完了まで待ちますが、対象 Task の終了は待ちません。
        /// 通知自身の同期コールバックから呼ばれた場合は自己待機を避け、通知の完了は TimeoutAsync が待ちます。
        /// 通知コールバックから別スレッドの Dispose を同期的に待つことは避けてください。
        /// </remarks>
        public void Dispose()
        {
            lock (gate)
            {
                disposed = true;
                current?.TimerCancellation.Cancel();
                while (current is { Notifying: true } operation
                    && operation.NotificationThreadId != Thread.CurrentThread.ManagedThreadId)
                    Monitor.Wait(gate);
            }
        }
    }
}
