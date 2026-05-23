using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;

namespace InteractionFlow.Standard.Builders
{
    public static class BuildFlowExtentions
    {
        public static FlowHandler<TContext> BuildFlow<TFocus, TContext>(this ScopeHandler parent)
            where TFocus : IProgramFlow<TContext>
            where TContext : IFlowContext
        {
            var builder = new FlowBuilder<TContext>();
            return builder.BuildFlow<TFocus>(parent);
        }
    }
}
