using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Focuses;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace InteractionFlow.Standard.Builders
{
    public class FocusBuilder<TContext> : ScopeServices, IFocusBuilder<TContext>
        where TContext : IFlowContext
    {
        private ScopeHandler BuildScope(params ScopeHandler[] parents)
        {
            var services = Services ?? throw new InvalidOperationException();
            try
            {
                var rootProvider = services.BuildServiceProvider();
                var scope = rootProvider.CreateScope();
                var scopedProvider = scope.ServiceProvider;
                return new ScopeHandler(scope, scopedProvider, parents);
            }
            finally
            {
                Services = null;
            }
        }

        public FocusHandler<TContext> BuildFocus<TFocus>(params ScopeHandler[] parents)
            where TFocus : IFocus<TContext>
        {
            var scope = BuildScope(parents);
            var focus = ActivatorUtilities.CreateInstance<TFocus>(scope)
                ?? throw new InvalidOperationException();

            return new FocusHandler<TContext>(scope, focus);
        }

        public FocusHandler<TContext> BuildFocus<TFocus>(object[] parameters, params ScopeHandler[] parents)
            where TFocus : IFocus<TContext>
        {
            var scope = BuildScope(parents);
            var focus = ActivatorUtilities.CreateInstance<TFocus>(scope, parameters)
                ?? throw new InvalidOperationException();

            return new FocusHandler<TContext>(scope, focus);
        }
    }
}
