using InteractionFlow.Core.Builders;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace InteractionFlow.Standard.Builders
{
    internal static class ScopeHandlerFactory
    {
        internal static ScopeHandler Create(IServiceCollection services, ScopeHandler[] parents)
        {
            var rootProvider = services.BuildServiceProvider();
            IDisposable lifetime = rootProvider;

            try
            {
                var scope = rootProvider.CreateScope();
                lifetime = new ScopeLifetime(scope, rootProvider);
                return new ScopeHandler(lifetime, scope.ServiceProvider, parents);
            }
            catch (Exception creationException)
            {
                try
                {
                    lifetime.Dispose();
                }
                catch (Exception disposalException)
                {
                    throw new AggregateException(creationException, disposalException);
                }

                throw;
            }
        }
    }
}
