namespace InteractionFlow.Core.Builders
{
    public interface IScopeServices
    {
        IScopeServices Use<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        IScopeServices Use<TService>()
            where TService : class;

        IScopeServices UseTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        IScopeServices UseTransient<TService>()
            where TService : class;

        IScopeServices Apply(IScopeProfile profile);
    }
}
