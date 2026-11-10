using InteractionFlow.Core.Builders;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Interactions;
using InteractionFlow.Standard.Builders;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Builders;

public sealed class LifetimeProfile : IScopeProfile
{
    public void Configure(IScopeServices services)
    {
        services.UseFunction<IShutdownReaction, ShutdownReaction>()
            .UseFunction<IExceptionPort<Exception>, GameExceptionHandling>()
            .UseFunction<ICancellationPort, GameCancellationHandling>()
            .UseInteraction<RequestShutdown>();
    }
}
