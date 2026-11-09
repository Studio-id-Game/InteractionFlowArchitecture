using InteractionFlow.Core.Builders;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Externals;
using InteractionFlow.Samples.EscapeGame.Interactions;
using InteractionFlow.Samples.EscapeGame.SystemFlows;
using InteractionFlow.Standard.Builders;
using System;

namespace InteractionFlow.Samples.EscapeGame.Builders;

public sealed class EscapeGameProfile : IScopeProfile
{
    public void Configure(IScopeServices services)
    {
        services.UseFunction<IWindowOperation, RaylibWindowOperation>()
            .UseFunction<IWindowReaction, RaylibWindowReaction>()
            .UseFunction<IGameOperation, GameOperation>()
            .UseFunction<IGameReaction, GameReaction>()
            .UseFunction<ISessionReaction, SessionReaction>()
            .UseFunction<IGameControlReaction, GameControlReaction>()
            .UseFunction<IExceptionPort<Exception>, GameExceptionHandling>()
            .UseFunction<ICancellationPort, GameCancellationHandling>()
            .UseInteraction<OpenWindow>()
            .UseInteraction<CloseWindow>()
            .UseInteraction<PresentFrame>()
            .UseInteraction<ChangeControl>()
            .UseInteraction<RequestShutdown>()
            .UseInteraction<BeginSession>()
            .UseInteraction<EndSession>()
            .UseInteraction<EnableSelection>()
            .UseInteraction<SelectAction>()
            .UseInteraction<ApplyPlayerAction>()
            .UseInteraction<ApplyEnemyAction>()
            .UseInteraction<ShowResult>()
            .UseInteraction<EnableResultInput>()
            .UseSystemFlow<TurnSystemFlow>()
            .UseSystemFlow<SessionSystemFlow>()
            .UseSystemFlow<GameSystemFlow>();
    }
}
