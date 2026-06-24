using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;

namespace InteractionFlow.Standard.Builders
{
    public static class BuildProgramFlowExtensions
    {
        public static ProgramFlowHandler<TContext> BuildProgramFlow<TProgramFlow, TContext>(this ScopeHandler parent)
            where TProgramFlow : IProgramFlow<TContext>
            where TContext : IFlowContext
        {
            var builder = new ProgramFlowBuilder<TContext>();
            return builder.BuildProgramFlow<TProgramFlow>(parent);
        }
    }
}
