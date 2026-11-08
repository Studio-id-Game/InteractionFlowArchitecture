using InteractionFlow.Core.Builders;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Raylib.ExternalPorts;
using InteractionFlow.Samples.Raylib.Externals;
using InteractionFlow.Samples.Raylib.Interactions;
using InteractionFlow.Samples.Raylib.SystemFlows;
using InteractionFlow.Standard.Builders;
using System;

namespace InteractionFlow.Samples.Raylib.Builders;

public sealed class RaylibCounterProfile : IScopeProfile
{
    public void Configure(IScopeServices services)
    {
        services.UseFunction<ICounterOperation, CounterOperation>()
            .UseFunction<ICounterReaction, CounterReaction>()
            .UseFunction<IWindowOperation, RaylibWindowOperation>()
            .UseFunction<IWindowReaction, RaylibWindowReaction>()
            .UseFunction<IExceptionPort<Exception>, CounterExceptionHandling>()
            .UseFunction<ICancellationPort, CounterCancellationHandling>()
            .UseInteraction<IncrementCounter>()
            .UseInteraction<OpenWindow>()
            .UseInteraction<PresentFrame>()
            .UseInteraction<CloseWindow>()
            .UseSystemFlow<CounterSystemFlow>();
    }
}
