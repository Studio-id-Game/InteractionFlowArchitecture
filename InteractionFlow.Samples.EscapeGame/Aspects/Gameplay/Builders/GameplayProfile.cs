using InteractionFlow.Core.Builders;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;
using InteractionFlow.Standard.Builders;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Builders;

public sealed class GameplayProfile : IScopeProfile
{
    public void Configure(IScopeServices services)
    {
        services.UseFunction<IGameplayReaction, GameplayReaction>()
            .UseFunction<IGameLifetimeReaction, GameLifetimeReaction>()
            .UseInteraction<BeginSession>()
            .UseInteraction<EndSession>()
            .UseInteraction<BeginActionSelection>()
            .UseInteraction<HandleGameInput>()
            .UseInteraction<ApplyPlayerAction>()
            .UseInteraction<ApplyEnemyAction>()
            .UseInteraction<PresentResult>();
    }
}
