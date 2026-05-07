using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Interactions
{
    public abstract class Interaction(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        params IFlowNode[] dependency)
        : Interaction<Exception>(exceptionPort, cancellationPort, dependency)
    {

    }

    public abstract class Interaction<TException>(
    IExceptionPort<TException> exceptionPort,
    ICancellationPort cancellationPort,
    params IFlowNode[] dependency)
    : IInteraction
    where TException : Exception
    {
        public ReadOnlySpan<IFlowNode> Dependency => (IFlowNode[])[exceptionPort, cancellationPort, .. dependency];

        public abstract Task<FlowEndToken> InteractWithUserAsync(IFlowContext context);

        protected async Task<FlowEndToken> HandleCancellationAsync(IFlowContext context, OperationCanceledException e)
        {
            return await cancellationPort.HandleCancellation(context, e);
        }

        protected async Task<FlowEndToken> HandleExceptionAsync(IFlowContext context, TException e)
        {
            return await exceptionPort.HandleExceptionAsync(context, e);
        }

        protected async Task<FlowEndToken> TryCatchBlock(IFlowContext context, Func<IFlowContext, Task<FlowEndToken>> function)
        {
            try
            {
                if (context.TryGetCanceledException(out var e))
                {
                    return await HandleCancellationAsync(context, e!);
                }

                return await function(context);
            }
            catch (OperationCanceledException e)
            {
                return await HandleCancellationAsync(context, e);
            }
            catch (TException e)
            {
                return await HandleExceptionAsync(context, e);
            }
        }
    }
}
