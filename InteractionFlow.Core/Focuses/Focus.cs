using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Interactions;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Focuses
{
    public abstract class Focus<TContext> : IFocus<TContext>, IUserFlowHandler<TContext>
        where TContext : IFlowContext
    {
        protected Focus(IUserFlowInvoker invoker)
        {
            this.invoker = invoker;
        }

        private readonly IUserFlowInvoker invoker;

        public async Task<FlowEndToken> UseUserFlowAsync(TContext context)
        {
            return await invoker.ExecuteUserFlowAsync(context, this);
        }

        protected abstract ValueTask<FlowEndToken> UserFlowCoreAsync(TContext context);

        protected ValueTask<FlowEndToken> InteractAndGetEndToken(IFlowContext context, IInteraction interaction)
        {
            return interaction.UseSystemFlowAsync(context);
        }

        ValueTask<FlowEndToken> IUserFlowHandler<TContext>.UserFlowCoreAsync(TContext context)
        {
            return UserFlowCoreAsync(context);
        }
    }
}
