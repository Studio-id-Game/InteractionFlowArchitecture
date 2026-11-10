using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Entities;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

internal static class GameTestSteps
{
    internal static GameplayContext CreateSessionContext(IGameplayContext app)
        => new(app, app.Gameplay.AllocateSession(), ContextInheritance.Cancellation);

    internal static GameplayContext Ready(IApplicationContext app)
    {
        var session = CreateSessionContext(app);
        var begin = session.Gameplay.BeginSession(session.SessionId, WorldRules.BeginSession);
        Assert.Equal(GameUpdateStatus.Applied, begin.Status);
        Complete(app, begin.Presentation!.Value);
        Assert.Equal(GameUpdateStatus.Applied,
            session.Gameplay.UpdateGameState(session.SessionId,
                (state, _, _, completed) => WorldRules.BeginActionSelection(state, completed)).Status);
        return session;
    }

    internal static void Complete(IViewContext context, PresentationKey key)
        => Assert.True(context.View.TryCompletePresentation(key));

    internal static bool SetPaused(this IGameplayAccess access, long id, bool paused)
        => access.UpdateGameState(id, (state, mode, presentation, completed) =>
            mode == (paused ? ExecutionMode.Paused : ExecutionMode.Active)
                ? new(state, new(false))
                : WorldRules.ApplyControl(state, new(id, GameControlCommand.TogglePause), mode, presentation, completed),
            allowWhilePaused: true).Status == GameUpdateStatus.Applied;

    internal static bool RequestSkip(this IGameplayAccess access, PresentationKey key)
        => access.UpdateGameState(key.SessionId, (state, mode, presentation, completed) =>
            WorldRules.ApplyControl(state, new(key.SessionId, GameControlCommand.Skip, key),
                mode, presentation, completed), allowWhilePaused: true).Status == GameUpdateStatus.Applied;

    internal static void PlayTurn(IGameplayContext session, IViewContext view, GameCommand command)
    {
        var id = session.SessionId;
        Assert.Equal(GameUpdateStatus.Applied,
            session.Gameplay.UpdateGameState(id,
                (state, _, _, _) => WorldRules.ApplyInputCommand(state, command)).Status);
        Assert.Equal(GameUpdateStatus.Applied,
            session.Gameplay.UpdateGameState(id,
                (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Confirm)).Status);
        var player = session.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyPlayer(state));
        Assert.Equal(GameUpdateStatus.Applied, player.Status);
        Complete(view, player.Presentation!.Value);
        if (session.Gameplay.ReadGameState(id).State.Outcome != GameOutcome.Running) return;
        var enemy = session.Gameplay.UpdateGameState(id,
            (state, _, _, completed) => WorldRules.ApplyEnemy(state, completed));
        Assert.Equal(GameUpdateStatus.Applied, enemy.Status);
        Complete(view, enemy.Presentation!.Value);
        if (session.Gameplay.ReadGameState(id).State.Outcome == GameOutcome.Running)
            Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.UpdateGameState(id,
                (state, _, _, completed) => WorldRules.BeginActionSelection(state, completed)).Status);
    }
}
