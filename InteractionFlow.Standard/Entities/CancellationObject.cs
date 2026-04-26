using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Entities
{
    public class CancellationObject
    {
        private CancellationTokenSource? tokenSource;

        public Task? CurrentTask { get; set; }

        public async void Cancel()
        {
            if (tokenSource == null)
                return;

            tokenSource.Cancel();
            tokenSource.Dispose();
            tokenSource = null;

            Task? task = null;
            task = Task.Run(async () =>
            {
                if (CurrentTask != null)
                {
                    await CurrentTask;
                    CurrentTask = null;
                    task?.Dispose();
                }
            });
        }

        public CancellationToken GetToken()
        {
            tokenSource ??= new();
            return tokenSource.Token;
        }
    }
}
