using InteractionFlow.Core.Builders;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Application.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Application.Externals;
using InteractionFlow.Samples.EscapeGame.Externals;
using InteractionFlow.Samples.EscapeGame.Gameplay.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Gameplay.Externals;
using InteractionFlow.Samples.EscapeGame.Gameplay.Interactions;
using InteractionFlow.Samples.EscapeGame.Lifetime.Interactions;
using InteractionFlow.Samples.EscapeGame.SystemFlows;
using InteractionFlow.Samples.EscapeGame.View.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.View.Externals;
using InteractionFlow.Samples.EscapeGame.View.Interactions;
using InteractionFlow.Standard.Builders;
using System;

namespace InteractionFlow.Samples.EscapeGame.Builders;

public sealed class EscapeGameProfile : IScopeProfile
{
    public void Configure(IScopeServices services)
    {
        services.UseFunction<IWindowOperation, RaylibWindowOperation>()
            .UseFunction<IWindowReaction, RaylibWindowReaction>()
            .UseFunction<IGameInputOperation, GameInputOperation>()
            .UseFunction<IGameplayReaction, GameplayReaction>()
            .UseFunction<IGameLifetimeReaction, GameLifetimeReaction>()
            .UseFunction<IGameControlReaction, GameControlReaction>()
            .UseFunction<IExceptionPort<Exception>, GameExceptionHandling>()
            .UseFunction<ICancellationPort, GameCancellationHandling>()
            .UseInteraction<OpenWindow>()
            .UseInteraction<CloseWindow>()
            .UseInteraction<PresentFrame>()
            .UseInteraction<HandleControlInput>()
            .UseInteraction<RequestShutdown>()
            .UseInteraction<BeginSession>()
            .UseInteraction<EndSession>()
            .UseInteraction<BeginActionSelection>()
            .UseInteraction<HandleGameInput>()
            .UseInteraction<ApplyPlayerAction>()
            .UseInteraction<ApplyEnemyAction>()
            .UseInteraction<PresentResult>()
            .UseSystemFlow<TurnSystemFlow>()
            .UseSystemFlow<SessionSystemFlow>()
            .UseSystemFlow<SessionLoopSystemFlow>();
    }
}
