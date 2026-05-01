using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    public static class IFlowContextExtensions
    {
        public static bool TryGetCanceledException(this IFlowContext context, out OperationCanceledException? canceledException)
        {
            var cancellation = context.Cancellation;

            if (cancellation.IsCancellationRequested)
            {
                var token = cancellation.GetToken();
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
