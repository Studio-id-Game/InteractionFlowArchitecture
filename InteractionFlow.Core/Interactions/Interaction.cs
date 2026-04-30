using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.MultiFunctionPorts;
using InteractionFlow.Core.ReactionPorts;
using System;
using System.Collections.Generic;
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

        public abstract IEnumerable<IFlowNodePortLayer> Ports { get; }

        public abstract Task<FlowEndToken> InteractWithUserAsync(IFlowContext context);

        protected ValueTask<FlowEndToken> EndInteractAsync(IFlowContext context, OperationCanceledException e)
        {
            return EndInteractAsync(context, cancellationPort, e);
        }

        protected ValueTask<FlowEndToken> EndInteractAsync(IFlowContext context, Exception e)
        {
            return EndInteractAsync(context, exceptionPort, e);
        }

        protected async ValueTask<FlowEndToken> EndInteractAsync<T>(IFlowContext context, IReactionPort<T> reaction, T reactionValue)
        {
            await reaction.ReactToUserAsync(context, reactionValue);
            return new FlowEndToken(context);
        }
    }
}
