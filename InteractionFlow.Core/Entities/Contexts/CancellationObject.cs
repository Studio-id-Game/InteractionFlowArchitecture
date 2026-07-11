using System;
using System.Collections.Concurrent;
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
        private CancellationTokenSource? tokenSource;

        private readonly ConcurrentBag<Task> currentTasks = [];

        /// <summary>
        /// 登録済みのキャンセル対象タスクが存在するかどうかを取得します。
        /// </summary>
        public bool HasTask => currentTasks.Any(e => !e.IsCompleted);

        /// <summary>
        /// 現在のキャンセルトークンにキャンセルが要求されているかどうかを取得します。
        /// </summary>
        public bool IsCancellationRequested => tokenSource?.IsCancellationRequested ?? false;

        /// <summary>
        /// キャンセル時に完了を待機する対象タスクを登録します。
        /// </summary>
        /// <param name="task">キャンセル後のリセット時に待機するタスク。</param>
        public void AddCancelableTask(Task task)
        {
            tokenSource ??= new();
            currentTasks.Add(task);
        }

        /// <summary>
        /// 現在のキャンセルトークンにキャンセルを要求します。
        /// </summary>
        public void Cancel()
        {
            tokenSource ??= new();

            if (!tokenSource.IsCancellationRequested)
                tokenSource.Cancel();
        }

        /// <summary>
        /// キャンセル要求がある場合、登録済みタスクをすべて待機してキャンセル状態をリセットします。
        /// </summary>
        /// <returns>リセットを実行した場合は <see langword="true"/>、キャンセル要求がない場合は <see langword="false"/>。</returns>
        public ValueTask<bool> TryWaitAndResetAsync()
        {
            if (tokenSource == null || !tokenSource.IsCancellationRequested)
                return new(false);

            return new(WaitAllAsync());

            async Task<bool> WaitAllAsync()
            {
                while (currentTasks.TryTake(out var task))
                {
                    await task.ConfigureAwait(false);
                }

                tokenSource?.Dispose();
                tokenSource = null;

                return true;
            }
        }

        /// <summary>
        /// 現在のキャンセルトークンを取得します。未作成の場合は新しく作成します。
        /// </summary>
        /// <returns>キャンセル制御に使用するトークン。</returns>
        public CancellationToken GetToken()
        {
            tokenSource ??= new();
            return tokenSource.Token;
        }

        /// <summary>
        /// キャンセル要求がある場合、そのトークンに紐づく <see cref="OperationCanceledException"/> を作成します。
        /// </summary>
        /// <param name="canceledException">キャンセル要求がある場合に作成された例外。</param>
        /// <returns>キャンセル要求がある場合は <see langword="true"/>。</returns>
        public bool TryGetCanceledException(out OperationCanceledException? canceledException)
        {
            if (IsCancellationRequested)
            {
                var token = GetToken();
                canceledException = new OperationCanceledException(token);
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
