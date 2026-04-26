using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Interactions
{
    public abstract class Interaction : IInteraction
    {
        protected Interaction(IExceptionPort exception, ICancellationPort cancellation)
        {
            ExceptionPort = exception;
            CancellationPort = cancellation;
        }

        public string Name => GetType().Name;

        private IExceptionPort ExceptionPort { get; }

        private ICancellationPort CancellationPort { get; }

        public async ValueTask<FlowEndToken> UseSystemFlowAsync(IFlowContext context)
        {
            try
            {
                context.CancellationToken.ThrowIfCancellationRequested();
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
            return ReactAndGetEndToken(context, CancellationPort, e);
        }

        protected virtual ValueTask<FlowEndToken> ExceptionInteractAsync(IFlowContext context, Exception e)
        {
            return ReactAndGetEndToken(context, ExceptionPort, e);
        }

        protected async ValueTask<FlowEndToken> ReactAndGetEndToken<T>(IFlowContext context, IReactionPort<T> reaction, T reactionValue)
        {
            await reaction.ReactToUserAsync(context, reactionValue);
            return new FlowEndToken(context);
        }

        async Task<FlowEndToken> IUserFlowInvoker.ExecuteUserFlowAsync<TContext>(TContext context, IUserFlowHandler<TContext> handler)
        {
            try
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                return await handler.UserFlowCoreAsync(context);
            }
            catch (OperationCanceledException e)
            {
                return await CancellationInteractAsync(context, e);
            }
            catch (Exception e)
            {
                return await ExceptionInteractAsync(context, e);
            }
        }
    }
}
