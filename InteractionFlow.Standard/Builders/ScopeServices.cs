using InteractionFlow.Core.Builders;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace InteractionFlow.Standard.Builders
{
    public abstract class ScopeServices : IScopeServices
    {
        protected ServiceCollection? Services { get; set; } = new();

        public IScopeServices Use<TService>()
            where TService : class
        {
            var services = Services ?? throw new InvalidOperationException();
            services.AddScoped<TService>();
            return this;
        }

        public IScopeServices Use<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            var services = Services ?? throw new InvalidOperationException();
            services.AddScoped<TService, TImplementation>();
            return this;
        }

        public IScopeServices UseTransient<TService>()
            where TService : class
        {
            var services = Services ?? throw new InvalidOperationException();
            services.AddTransient<TService>();
            return this;
        }

        public IScopeServices UseTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            var services = Services ?? throw new InvalidOperationException();
            services.AddTransient<TService, TImplementation>();
            return this;
        }

        public IScopeServices Apply(IScopeProfile profile)
        {
            profile.Configure(this);
            return this;
        }
    }
}
