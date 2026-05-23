using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace InteractionFlow.Standard.Builders
{
    public class FlowBuilder<TContext> : ScopeServices, IFlowBuilder<TContext>
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

        public FlowHandler<TContext> BuildFlow<TFocus>(params ScopeHandler[] parents)
            where TFocus : IProgramFlow<TContext>
        {
            var scope = BuildScope(parents);
            var focus = ActivatorUtilities.CreateInstance<TFocus>(scope)
                ?? throw new InvalidOperationException();

            return new FlowHandler<TContext>(scope, focus);
        }

        public FlowHandler<TContext> BuildFlow<TFocus>(object[] parameters, params ScopeHandler[] parents)
            where TFocus : IProgramFlow<TContext>
        {
            var scope = BuildScope(parents);
            var focus = ActivatorUtilities.CreateInstance<TFocus>(scope, parameters)
                ?? throw new InvalidOperationException();

            return new FlowHandler<TContext>(scope, focus);
        }
    }
}
