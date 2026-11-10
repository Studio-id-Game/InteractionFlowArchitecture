using InteractionFlow.Samples.EscapeGame.Aspects.Shared.ExternalPorts;
using InteractionFlow.Core.Builders;
using InteractionFlow.Samples.EscapeGame.Aspects.View.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Interactions;
using InteractionFlow.Standard.Builders;

namespace InteractionFlow.Samples.EscapeGame.Aspects.View.Builders;

public sealed class ViewProfile : IScopeProfile
{
    public void Configure(IScopeServices services)
    {
        services.UseFunction<IWindowOperation, RaylibWindowOperation>()
            .UseFunction<IWindowReaction, RaylibWindowReaction>()
            .UseFunction<IGameControlReaction, GameControlReaction>()
            .UseInteraction<OpenWindow>()
            .UseInteraction<PresentFrame>()
            .UseInteraction<HandleControlInput>();
    }
}
