using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Interactions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Focuses
{
    public abstract class Focus<TContext> : IFocus<TContext>
        where TContext : IFlowContext
    {
        public abstract IEnumerable<IInteraction> Interactions { get; }

        public abstract Task<FlowEndToken> FlowWithUserAsync(TContext context);

        protected Task<FlowEndToken> EndFlowAsync(IFlowContext context, IInteraction interaction)
        {
            return interaction.InteractWithUserAsync(context);
        }
    }
}
