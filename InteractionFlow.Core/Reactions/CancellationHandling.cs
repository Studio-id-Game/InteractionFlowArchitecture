using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Reactions
{
    public abstract class CancellationHandling(params IFlowNode[] dependency) : ExceptionHandling<OperationCanceledException>(dependency), ICancellationPort
    {
        public ValueTask<FlowEndToken> HandleCancellationAsync(IFlowContext context, OperationCanceledException exception)
        {
            var _BeforeCancellationCoreAsync = BeforeCancellationCoreAsync(context, exception);
            if (!_BeforeCancellationCoreAsync.IsCompletedSuccessfully)
            {
                return SlowPathAsync(this, _BeforeCancellationCoreAsync, context, exception);

                static async ValueTask<FlowEndToken> SlowPathAsync(CancellationHandling @this, ValueTask before, IFlowContext context, OperationCanceledException exception)
                {
                    await before;
                    await context.Cancellation.TryWaitAndResetAsync();
                    return await @this.AfterCancellationCoreAsync(context, exception);
                }
            }

            var _TryWaitAndResetAsync = context.Cancellation.TryWaitAndResetAsync();
            if (!_TryWaitAndResetAsync.IsCompletedSuccessfully)
            {
                return SlowPathAsync(this, _TryWaitAndResetAsync, context, exception);

                static async ValueTask<FlowEndToken> SlowPathAsync(CancellationHandling @this, ValueTask<bool> before, IFlowContext context, OperationCanceledException exception)
                {
                    await before;
                    return await @this.AfterCancellationCoreAsync(context, exception);
                }
            }

            return AfterCancellationCoreAsync(context, exception);
        }

        protected sealed override ValueTask<FlowEndToken> HandleExceptionCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            return HandleCancellationAsync(context, exception);
        }

        protected virtual ValueTask BeforeCancellationCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            return default;
        }

        protected abstract ValueTask<FlowEndToken> AfterCancellationCoreAsync(IFlowContext context, OperationCanceledException exception);
    }
}
