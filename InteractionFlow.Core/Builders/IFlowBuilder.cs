using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;

namespace InteractionFlow.Core.Builders
{
    public interface IFlowBuilder<TContext> : IScopeServices
        where TContext : IFlowContext
    {
        FlowHandler<TContext> BuildFlow<TFocus>(object[] parameters, params ScopeHandler[] parents)
            where TFocus : IProgramFlow<TContext>;

        FlowHandler<TContext> BuildFlow<TFocus>(params ScopeHandler[] parents)
            where TFocus : IProgramFlow<TContext>;
    }
}
