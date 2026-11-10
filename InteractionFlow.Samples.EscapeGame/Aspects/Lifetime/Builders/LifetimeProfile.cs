using InteractionFlow.Core.Builders;
using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Interactions;
using InteractionFlow.Standard.Builders;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Builders;

public sealed class LifetimeProfile : IScopeProfile
{
    public void Configure(IScopeServices services)
    {
        services.UseInteraction<CloseWindow>()
            .UseInteraction<RequestShutdown>();
    }
}
