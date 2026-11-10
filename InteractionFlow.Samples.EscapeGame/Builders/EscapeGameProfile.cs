using InteractionFlow.Core.Builders;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.Builders;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Builders;
using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Builders;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Builders;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Builders;
using InteractionFlow.Samples.EscapeGame.SystemFlows;
using InteractionFlow.Standard.Builders;

namespace InteractionFlow.Samples.EscapeGame.Builders;

public sealed class EscapeGameProfile : IScopeProfile
{
    public void Configure(IScopeServices services)
    {
        services.Apply(new SharedProfile())
            .Apply(new ApplicationProfile())
            .Apply(new GameplayProfile())
            .Apply(new ViewProfile())
            .Apply(new LifetimeProfile())
            .UseSystemFlow<GameSessionSystemFlow>()
            .UseSystemFlow<GamePlaySystemFlow>();
    }
}
