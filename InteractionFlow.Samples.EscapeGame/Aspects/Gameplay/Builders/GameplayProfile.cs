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
            .UseFunction<IGameInputOperation, GameInputOperation>()
            .UseFunction<IGameSessionReaction, GameSessionReaction>()
            .UseInteraction<HandleControlInput>()
            .UseInteraction<BeginSession>()
            .UseInteraction<EndSession>()
            .UseInteraction<PlayTurn>()
            .UseInteraction<HandleResult>();
    }
}
