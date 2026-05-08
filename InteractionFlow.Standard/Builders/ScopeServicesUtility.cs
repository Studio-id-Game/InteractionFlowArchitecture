using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Focuses;
using InteractionFlow.Core.Interactions;
using System.Runtime.CompilerServices;

namespace InteractionFlow.Standard.Builders
{
    public static class ScopeServicesUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IScopeServices UseInteraction<TImplementation>(this IScopeServices @this)
            where TImplementation : class, IInteraction
        {

            return @this.Use<TImplementation>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IScopeServices UseFocus<TImplementation>(this IScopeServices @this)
            where TImplementation : class, IFocus
        {

            return @this.Use<TImplementation>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IScopeServices UseFunction<TService, TImplementation>(this IScopeServices @this)
            where TService : class, IFlowNodeStateful
            where TImplementation : class, TService
        {
            return @this.Use<TService, TImplementation>();
        }
    }
}
