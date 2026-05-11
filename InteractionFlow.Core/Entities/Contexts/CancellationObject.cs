using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Entities.Contexts
{
    public class CancellationObject
    {
        private CancellationTokenSource? tokenSource;

        private readonly ConcurrentBag<Task> currentTasks = [];

        public bool HasTask => currentTasks.Any();

        public bool IsCancellationRequested => tokenSource?.IsCancellationRequested ?? false;

        public void AddCancelableTask(Task task)
        {
            tokenSource ??= new();
            currentTasks.Add(task);
        }

        public void Cancel()
        {
            tokenSource ??= new();

            if (!tokenSource.IsCancellationRequested)
                tokenSource.Cancel();
        }

        public ValueTask<bool> TryWaitAndResetAsync()
        {
            if (tokenSource == null || !tokenSource.IsCancellationRequested)
                return new(false);

            return new(WaitAllAsync());

            async Task<bool> WaitAllAsync()
            {
                while (currentTasks.TryTake(out var task))
                {
                    await task;
                }

                tokenSource?.Dispose();
                tokenSource = null;

                return true;
            }
        }

        public CancellationToken GetToken()
        {
            tokenSource ??= new();
            return tokenSource.Token;
        }
    }
}
