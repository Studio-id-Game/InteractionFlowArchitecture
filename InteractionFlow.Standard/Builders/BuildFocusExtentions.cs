using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Focuses;

namespace InteractionFlow.Standard.Builders
{
    public static class BuildFocusExtentions
    {
        public static FocusHandler<TContext> BuildFocus<TFocus, TContext>(this ScopeHandler parent)
            where TFocus : IFocus<TContext>
            where TContext : IFlowContext
        {
            var builder = new FocusBuilder<TContext>();
            return builder.BuildFocus<TFocus>(parent);
        }
    }
}
