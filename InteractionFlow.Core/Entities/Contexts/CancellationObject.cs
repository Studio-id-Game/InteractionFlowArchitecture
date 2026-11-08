using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>キャンセル要求と、登録 Task の完了待機・キャンセル状態のリセットを管理します。</summary>
    public class CancellationObject
    {
        private sealed class CancellationState
        {
            public CancellationTokenSource Source { get; } = new();
            public int ActiveNotifications { get; set; }
            public bool Retired { get; set; }
        }

        private sealed class TaskRegistration(CancellationObject owner, Task task) : IDisposable
        {
            private CancellationObject? owner = owner;
            public Task? Task { get; private set; } = task;
            public bool FailureCollected { get; set; }

            public void Dispose()
            {
                var currentOwner = owner;
                if (currentOwner is null) return;
                lock (currentOwner.gate)
                {
                    if (owner is null) return;
                    currentOwner.registrations.Remove(this);
                    Detach();
                }
            }

            // Called only while holding the owner's gate.
            public void Detach()
            {
                owner = null;
                Task = null;
            }
        }

        private readonly object gate = new();
        private readonly List<TaskRegistration> registrations = [];
        private CancellationState? state;
        private Task[]? waitingTasks;

        /// <summary>現在の登録、または確定済みの待機対象に未完了 Task があるかを返します。</summary>
        public bool HasTask
        {
            get
            {
                lock (gate)
                {
                    return waitingTasks != null
                        ? waitingTasks.Any(task => !task.IsCompleted)
                        : registrations.Any(registration => !registration.Task!.IsCompleted);
                }
            }
        }

        /// <summary>現在のキャンセル要求状態。登録 Task の待機中も true を維持します。</summary>
        public bool IsCancellationRequested
        {
            get { lock (gate) { return state?.Source.IsCancellationRequested ?? false; } }
        }

        /// <summary>キャンセル時の待機対象へ登録し、登録解除用のスコープを返します。</summary>
        /// <remarks>
        /// 所有者が Task の結果を受け取り、返却スコープを破棄してください。
        /// Dispose は登録解除だけを行い、Task のキャンセル・待機・破棄は行いません。
        /// 同じ Task の複数登録は独立しています。待機開始後の登録は拒否します。
        /// </remarks>
        public IDisposable AddCancelableTask(Task task)
        {
            if (task is null) throw new ArgumentNullException(nameof(task));
            lock (gate)
            {
                if (waitingTasks != null)
                    throw new InvalidOperationException(
                        "Cannot register tasks while cancellation reset is in progress.");
                state ??= new CancellationState();
                var registration = new TaskRegistration(this, task);
                registrations.Add(registration);
                return registration;
            }
        }

        /// <summary>現在のキャンセル元へ同期的に通知します。Task の待機・リセットは行いません。</summary>
        /// <remarks>通知先の例外は呼び出し元へ伝播します。通知中のキャンセル元はリセット時にも破棄しません。</remarks>
        public void Cancel() => Cancel(expectedState: null);

        // Capture the state, not its token/source: a retired state's source may already be disposed at deadline.
        internal Action CaptureCancellationRequest()
        {
            lock (gate)
            {
                var captured = state ??= new CancellationState();
                return () => Cancel(captured);
            }
        }

        private void Cancel(CancellationState? expectedState)
        {
            CancellationState current;
            lock (gate)
            {
                // Check identity and pin the notification in one critical section against FinishReset.
                if (expectedState != null && !ReferenceEquals(expectedState, state)) return;
                current = state ??= new CancellationState();
                if (current.Source.IsCancellationRequested) return;
                current.ActiveNotifications++;
            }
            try
            {
                // User callbacks may re-enter this object; never invoke them while holding gate.
                current.Source.Cancel();
            }
            finally
            {
                lock (gate)
                {
                    current.ActiveNotifications--;
                    if (current.Retired && current.ActiveNotifications == 0) current.Source.Dispose();
                }
            }
        }

        /// <summary>登録 Task の終了をすべて待ち、失敗を集約してからキャンセル状態をリセットします。</summary>
        /// <returns>正常・キャンセル完了は成功。Faulted Task の例外は AggregateException に保持します。</returns>
        /// <remarks>
        /// 要求なし・重複リセットは失敗 Result です。待機中の新規登録は拒否します。
        /// 待機開始後に登録スコープが破棄されても、確定済みの Task は待ちます。
        /// 通知中の Source の実際の破棄は、最後の Cancel 呼び出しの終了まで遅延します。
        /// </remarks>
        public ValueTask<Result> WaitForTasksAndResetCancellationAsync()
        {
            CancellationState current;
            Task[] tasks;
            TaskRegistration[] snapshot;
            lock (gate)
            {
                if (waitingTasks != null)
                    return new(new InvalidOperationException("Cancellation reset is already in progress."));
                if (state is null || !state.Source.IsCancellationRequested)
                    return new(new InvalidOperationException("Cancellation is not requested."));
                current = state;
                snapshot = [.. registrations];
                tasks = [.. snapshot.Select(registration => registration.Task!)];
                waitingTasks = tasks;
            }

            if (!tasks.All(task => task.IsCompleted)) return new(WaitAndResetCoreAsync(current, snapshot, tasks));
            try { return new(CollectFailures(snapshot, tasks)); }
            finally { FinishReset(current); }
        }

        private async Task<Result> WaitAndResetCoreAsync(
            CancellationState current, TaskRegistration[] snapshot, Task[] tasks)
        {
            try
            {
                foreach (var task in tasks)
                {
                    try { await task.ConfigureAwait(false); }
                    catch (Exception)
                    {
                        // Inspect final Task states below, including faulted OperationCanceledExceptions.
                    }
                }
                return CollectFailures(snapshot, tasks);
            }
            finally { FinishReset(current); }
        }

        private Result CollectFailures(TaskRegistration[] snapshot, Task[] tasks)
        {
            List<Exception>? failures = null;
            foreach (var task in tasks)
            {
                if (!task.IsFaulted) continue;
                failures ??= [];
                failures.AddRange(task.Exception!.InnerExceptions);
            }
            Result result = failures is null ? Result.Success : new AggregateException(failures);
            lock (gate)
            {
                // Record transfer to the wait result, not whether a port chose to retain or recover the failure.
                for (var i = 0; i < tasks.Length; i++)
                    if (tasks[i].IsFaulted) snapshot[i].FailureCollected = true;
            }
            return result;
        }

        internal bool HasCollectedTaskFailure(IDisposable registration)
        {
            lock (gate) { return registration is TaskRegistration task && task.FailureCollected; }
        }

        private void FinishReset(CancellationState current)
        {
            lock (gate)
            {
                foreach (var registration in registrations) registration.Detach();
                registrations.Clear();
                waitingTasks = null;
                state = null;
                current.Retired = true;
                if (current.ActiveNotifications == 0) current.Source.Dispose();
            }
        }

        /// <summary>登録 Task の終了を待ち、キャンセル状態をリセットします。</summary>
        [Obsolete("Use WaitForTasksAndResetCancellationAsync instead.")]
        public ValueTask<Result> WaitAndResetAsync() => WaitForTasksAndResetCancellationAsync();

        /// <summary>現在のトークンを取得します。待機・リセット中はキャンセル済みのトークンを返します。</summary>
        public CancellationToken GetToken()
        {
            lock (gate)
            {
                state ??= new CancellationState();
                return state.Source.Token;
            }
        }

        /// <summary>要求済みの場合、現在のトークンに紐づくキャンセル例外を作成します。</summary>
        public bool TryGetCanceledException([MaybeNullWhen(false)] out OperationCanceledException canceledException)
        {
            lock (gate)
            {
                if (state?.Source.IsCancellationRequested ?? false)
                {
                    canceledException = new OperationCanceledException(state.Source.Token);
                    return true;
                }
                canceledException = null;
                return false;
            }
        }
    }
}
