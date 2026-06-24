using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace InteractionFlow.Standard.Builders
{
    public class ProgramFlowBuilder<TContext> : ScopeServices, IProgramFlowBuilder<TContext>
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

        public ProgramFlowHandler<TContext> BuildProgramFlow<TProgramFlow>(params ScopeHandler[] parents)
            where TProgramFlow : IProgramFlow<TContext>
        {
            var scope = BuildScope(parents);
            var programFlow = ActivatorUtilities.CreateInstance<TProgramFlow>(scope)
                ?? throw new InvalidOperationException();

            return new ProgramFlowHandler<TContext>(scope, programFlow);
        }

        public ProgramFlowHandler<TContext> BuildProgramFlow<TProgramFlow>(object[] parameters, params ScopeHandler[] parents)
            where TProgramFlow : IProgramFlow<TContext>
        {
            var scope = BuildScope(parents);
            var programFlow = ActivatorUtilities.CreateInstance<TProgramFlow>(scope, parameters)
                ?? throw new InvalidOperationException();

            return new ProgramFlowHandler<TContext>(scope, programFlow);
        }
    }
}
