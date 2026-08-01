using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// フロー実行中のキャンセル要求と、キャンセル対象タスクの待機・リセットを管理します。
    /// </summary>
    public class CancellationObject
    {
        // 通常のフローでは登録タスク数は少ないため、毎回走査せず、
        // 完了済みタスクの保持コストが走査コストを上回り始める目安として 64 件から取り除く。
        private const int CompactTaskCountThreshold = 64;

        private readonly object lockObject = new();

        private CancellationTokenSource? tokenSource;

        private readonly List<Task> currentTasks = [];

        /// <summary>
        /// 登録済みのキャンセル対象タスクが存在するかどうかを取得します。
        /// </summary>
        public bool HasTask
        {
            get
            {
                lock (lockObject)
                {
                    return currentTasks.Any(e => !e.IsCompleted);
                }
            }
        }

        /// <summary>
        /// 現在のキャンセルトークンにキャンセルが要求されているかどうかを取得します。
        /// </summary>
        public bool IsCancellationRequested
        {
            get
            {
                lock (lockObject)
                {
                    return tokenSource?.IsCancellationRequested ?? false;
                }
            }
        }

        /// <summary>
        /// キャンセル時に完了を待機する対象タスクを登録します。
        /// </summary>
        /// <param name="task">キャンセル後のリセット時に待機するタスク。</param>
        public void AddCancelableTask(Task task)
        {
            lock (lockObject)
            {
                tokenSource ??= new();

                if (currentTasks.Count >= CompactTaskCountThreshold)
                {
                    currentTasks.RemoveAll(e => e.IsCompleted);
                }

                currentTasks.Add(task);
            }
        }

        /// <summary>
        /// 現在のキャンセルトークンにキャンセルを要求します。
        /// </summary>
        public void Cancel()
        {
            CancellationTokenSource source;

            lock (lockObject)
            {
                tokenSource ??= new();
                source = tokenSource;
            }

            if (!source.IsCancellationRequested)
                source.Cancel();
        }

        /// <summary>
        /// キャンセル要求がある場合、登録済みタスクをすべて待機してキャンセル状態をリセットします。
        /// </summary>
        /// <returns>
        /// 待機とリセットの結果。
        /// キャンセル要求がない場合、登録タスクが正常完了した場合、または登録タスクがキャンセル完了した場合は成功結果を返します。
        /// 登録タスクが通常例外で失敗した場合は失敗結果を返します。
        /// </returns>
        public ValueTask<Result> WaitAndResetAsync()
        {
            CancellationTokenSource source;
            Task[] tasks;
            bool isCompleted;

            lock (lockObject)
            {
                if (tokenSource == null || !tokenSource.IsCancellationRequested)
                    return new(new InvalidOperationException("Cancellation is not requested."));

                source = tokenSource;
                tasks = [.. currentTasks];
                currentTasks.Clear();
                tokenSource = null;
                isCompleted = tasks.Length == 0 || tasks.All(e => e.IsCompleted);
            }

            if (isCompleted)
            {
                source.Dispose();

                return new(GetCompletedResult(tasks));
            }

            return new(WaitAllAsync(source, tasks));

            static Result GetCompletedResult(Task[] tasks)
            {
                var exceptions = tasks
                    .Where(e => e.IsFaulted)
                    .SelectMany(e => e.Exception?.InnerExceptions ?? Enumerable.Empty<Exception>())
                    .ToArray();

                if (exceptions.Length == 0)
                    return Result.Success;

                return new AggregateException(exceptions);
            }

            static async Task<Result> WaitAllAsync(CancellationTokenSource source, Task[] tasks)
            {
                try
                {
                    List<Exception>? exceptions = null;

                    foreach (var task in tasks)
                    {
                        try
                        {
                            await task.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (Exception e)
                        {
                            exceptions ??= [];

                            if (task.Exception == null)
                            {
                                exceptions.Add(e);
                            }
                            else
                            {
                                exceptions.AddRange(task.Exception.InnerExceptions);
                            }
                        }
                    }

                    if (exceptions == null || exceptions.Count == 0)
                        return Result.Success;

                    return new AggregateException(exceptions);
                }
                finally
                {
                    source.Dispose();
                }
            }
        }

        /// <summary>
        /// 現在のキャンセルトークンを取得します。未作成の場合は新しく作成します。
        /// </summary>
        /// <returns>キャンセル制御に使用するトークン。</returns>
        public CancellationToken GetToken()
        {
            lock (lockObject)
            {
                tokenSource ??= new();
                return tokenSource.Token;
            }
        }

        /// <summary>
        /// キャンセル要求がある場合、そのトークンに紐づく <see cref="OperationCanceledException"/> を作成します。
        /// </summary>
        /// <param name="canceledException">キャンセル要求がある場合に作成された例外。</param>
        /// <returns>キャンセル要求がある場合は <see langword="true"/>。</returns>
        public bool TryGetCanceledException(out OperationCanceledException? canceledException)
        {
            lock (lockObject)
            {
                if (tokenSource?.IsCancellationRequested ?? false)
                {
                    canceledException = new OperationCanceledException(tokenSource.Token);
                    return true;
                }
                else
                {
                    canceledException = null;
                    return false;
                }
            }
        }
    }
}
