using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Entities.Contexts
{
    public class CancellationObject
    {
        private CancellationTokenSource? tokenSource;

        private readonly ConcurrentBag<Task> currentTasks = new();

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

        public async Task<bool> TryWaitAndReset()
        {
            if (tokenSource == null || !tokenSource.IsCancellationRequested)
                return false;

            await Task.WhenAll(currentTasks);
            currentTasks.Clear();

            tokenSource?.Dispose();
            tokenSource = null;

            return true;
        }

        public CancellationToken GetToken()
        {
            tokenSource ??= new();
            return tokenSource.Token;
        }
    }
}
