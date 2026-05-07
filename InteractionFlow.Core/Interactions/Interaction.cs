using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ReactionPorts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Interactions
{
    public abstract class Interaction(IExceptionPort exceptionPort, ICancellationPort cancellationPort) : IInteraction
    {
        public virtual IEnumerable<IFlowNodePortLayer> Ports
        {
            get
            {
                yield return exceptionPort;
                yield return cancellationPort;
            }
        }

        public abstract Task<FlowEndToken> InteractWithUserAsync(IFlowContext context);

        protected Task<FlowEndToken> EndInteractAsync(IFlowContext context, OperationCanceledException e)
        {
            return EndInteractAsync(context, cancellationPort, e)
                .ContinueWith(t =>
                {
                    var token = t.Result;
                    token.Exception = e;
                    return token;
                });
        }

        protected Task<FlowEndToken> EndInteractAsync(IFlowContext context, Exception e)
        {
            return EndInteractAsync(context, exceptionPort, e)
                .ContinueWith(t =>
                {
                    var token = t.Result;
                    token.Exception = e;
                    return token;
                });
        }

        protected async Task<FlowEndToken> EndInteractAsync<T>(IFlowContext context, IReactionPort<T> reaction, T reactionValue)
        {
            await reaction.ReactToUserAsync(context, reactionValue);
            return new FlowEndToken(context);
        }

        protected async Task<FlowEndToken> TryCatchBlock(IFlowContext context, Func<IFlowContext, Task<FlowEndToken>> function)
        {
            try
            {
                if (context.TryGetCanceledException(out var e))
                {
                    return await EndInteractAsync(context, e!);
                }

                return await function(context);
            }
            catch (OperationCanceledException e)
            {
                return await EndInteractAsync(context, e);
            }
            catch (Exception e)
            {
                return await EndInteractAsync(context, e);
            }
        }
    }
}
