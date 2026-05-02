using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Focuses;

namespace InteractionFlow.Core.Builders
{
    public interface IFocusBuilder<TContext> : IScopeServices
        where TContext : IFlowContext
    {
        FocusHandler<TContext> BuildFocus<TFocus>(object[] parameters, params ScopeHandler[] parents)
            where TFocus : IFocus<TContext>;

        FocusHandler<TContext> BuildFocus<TFocus>(params ScopeHandler[] parents)
            where TFocus : IFocus<TContext>;
    }
}
