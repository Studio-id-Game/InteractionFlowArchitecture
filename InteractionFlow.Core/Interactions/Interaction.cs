using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Interactions
{
    public abstract class Interaction : IInteraction
    {
        protected readonly IExceptionPort exceptionPort;

        protected readonly ICancellationPort cancellationPort;

        protected Interaction(IExceptionPort exceptionPort, ICancellationPort cancellationPort)
        {
            this.exceptionPort = exceptionPort;
            this.cancellationPort = cancellationPort;
        }

        public string Name => GetType().Name;

        public async ValueTask<FlowEndToken> UseSystemFlowAsync(IFlowContext context)
        {
            var cancellationToken = context.Cancellation.GetToken();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await SystemFlowCoreAsync(context);
            }
            catch (OperationCanceledException e)
            {
                var e2 = new InteractionCanceledException(this, e);
                var end = await CancellationInteractAsync(context, e2);
                end.CanceledException = e2;
                return end;
            }
            catch (Exception e)
            {
                var e2 = new InteractionException(this, e);
                var end = await ExceptionInteractAsync(context, e2);
                end.Exception = e2;
                return end;
            }
        }

        protected abstract ValueTask<FlowEndToken> SystemFlowCoreAsync(IFlowContext context);

        protected virtual ValueTask<FlowEndToken> CancellationInteractAsync(IFlowContext context, OperationCanceledException e)
        {
            return ReactAndGetEndToken(context, cancellationPort, e);
        }

        protected virtual ValueTask<FlowEndToken> ExceptionInteractAsync(IFlowContext context, Exception e)
        {
            return ReactAndGetEndToken(context, exceptionPort, e);
        }

        protected async ValueTask<FlowEndToken> ReactAndGetEndToken<T>(IFlowContext context, IReactionPort<T> reaction, T reactionValue)
        {
            await reaction.ReactToUserAsync(context, reactionValue);
            return new FlowEndToken(context);
        }

        async Task<FlowEndToken> IUserFlowInvoker.ExecuteUserFlowAsync<TContext>(TContext context, IUserFlowHandler<TContext> handler)
        {
            var cancellationToken = context.Cancellation.GetToken();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await handler.UserFlowCoreAsync(context);
            }
            catch (OperationCanceledException e)
            {
                var e2 = new InteractionCanceledException(this, e);
                var end = await CancellationInteractAsync(context, e2);
                end.CanceledException = e2;
                return end;
            }
            catch (Exception e)
            {
                var e2 = new InteractionException(this, e);
                var end = await ExceptionInteractAsync(context, e2);
                end.Exception = e2;
                return end;
            }
        }
    }
}
