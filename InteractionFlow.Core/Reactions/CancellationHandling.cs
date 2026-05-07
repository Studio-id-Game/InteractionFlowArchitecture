using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Reactions
{
    public abstract class CancellationHandling(params IFlowNode[] dependency) : ExceptionHandling<OperationCanceledException>(dependency), ICancellationPort
    {
        public ValueTask<FlowEndToken> HandleCancellation(IFlowContext context, OperationCanceledException exception)
        {
            return HandleExceptionAsync(context, exception);
        }

        protected sealed override async ValueTask<FlowEndToken> HandleExceptionCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            await BeforeCancellationCoreAsync(context, exception);
            await context.Cancellation.TryWaitAndReset();
            return await AfterCancellationCoreAsync(context, exception);
        }

        protected virtual ValueTask BeforeCancellationCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            return default;
        }

        protected abstract ValueTask<FlowEndToken> AfterCancellationCoreAsync(IFlowContext context, OperationCanceledException exception);
    }
}
