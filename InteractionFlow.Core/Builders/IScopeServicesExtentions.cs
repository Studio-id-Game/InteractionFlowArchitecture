using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Focuses;
using InteractionFlow.Core.Interactions;

namespace InteractionFlow.Core.Builders
{
    public static class IScopeServicesExtentions
    {
        public static IScopeServices UseInteraction<TImplementation>(this IScopeServices @this)
            where TImplementation : class, IInteraction
        {

            return @this.Use<TImplementation>();
        }

        public static IScopeServices UseFocus<TImplementation>(this IScopeServices @this)
            where TImplementation : class, IFocus
        {

            return @this.Use<TImplementation>();
        }

        public static IScopeServices UseFunction<TService, TImplementation>(this IScopeServices @this)
            where TService : class, IFlowNodePortLayer
            where TImplementation : class, TService
        {
            return @this.Use<TService, TImplementation>();
        }
    }
}
