using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;

namespace InteractionFlow.Core.Builders
{
    public interface IProgramFlowBuilder<TContext> : IScopeServices
        where TContext : IFlowContext
    {
        ProgramFlowHandler<TContext> BuildProgramFlow<TProgramFlow>(object[] parameters, params ScopeHandler[] parents)
            where TProgramFlow : IProgramFlow<TContext>;

        ProgramFlowHandler<TContext> BuildProgramFlow<TProgramFlow>(params ScopeHandler[] parents)
            where TProgramFlow : IProgramFlow<TContext>;
    }
}
