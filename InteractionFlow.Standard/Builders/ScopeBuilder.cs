using InteractionFlow.Core.Builders;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace InteractionFlow.Standard.Builders
{
    public class ScopeBuilder : ScopeServices
    {
        public ScopeHandler BuildScope(params ScopeHandler[] parents)
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
    }
}
