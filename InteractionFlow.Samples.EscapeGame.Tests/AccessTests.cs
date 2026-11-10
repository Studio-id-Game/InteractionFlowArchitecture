using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class AccessTests
{
    [Fact]
    public void ConfirmedActionAdvancesWithoutAnotherInputWait()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Up));
        session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Confirm));

        Assert.False(app.View.ReadGameState().Info.WaitingForInput);

        Assert.False(app.View.ReadGameState().Info.WaitingForInput);
        Assert.True(session.Gameplay.ReadGameState(id).State.Confirmed);
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyPlayer(state)).Status);
    }

    [Fact]
    public void PauseBeforeCommitPreservesConfirmedActionAndPreventsDoubleApplication()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Up));
        session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Confirm));
        var token = session.Cancellation.GetToken();
        Assert.True(app.Gameplay.SetPaused(id, true));
        Assert.Equal(GameUpdateStatus.Deferred, session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyPlayer(state)).Status);
        Assert.True(session.Gameplay.ReadGameState(id).State.Confirmed);
        Assert.Equal(0, app.View.ReadGameState().State.Turn);
        Assert.True(app.Gameplay.SetPaused(id, false));
        var applied = session.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyPlayer(state));
        Assert.Equal(GameUpdateStatus.Applied, applied.Status);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyPlayer(state)).Status);
        Assert.Equal(1, app.View.ReadGameState().State.Turn);
        Assert.Equal(token, session.Cancellation.GetToken());
        Assert.Equal(id, app.View.ReadGameState().State.SessionId);
    }

    [Fact]
    public async Task OldPresentationKeyCannotSkipOrCompleteNextPresentationInSameSession()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Up));
        session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Confirm));
        var player = session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyPlayer(state)).Presentation!.Value;
        GameTestSteps.Complete(app, player);
        var enemy = session.Gameplay.UpdateGameState(id,
            (state, _, _, completed) => WorldRules.ApplyEnemy(state, completed)).Presentation!.Value;
        var waiting = session.Gameplay.WaitForPresentationCompletionAsync(enemy, TestContext.Current.CancellationToken).AsTask();
        Assert.False(app.Gameplay.RequestSkip(player));
        Assert.False(app.View.TryCompletePresentation(player));
        Assert.False(app.View.TryCompletePresentation(enemy with { Version = enemy.Version - 1 }));
        Assert.False(app.View.ReadGameState().Info.SkipRequested);
        Assert.False(waiting.IsCompleted);
        Assert.True(app.Gameplay.RequestSkip(enemy));
        GameTestSteps.Complete(app, enemy);
        Assert.Equal(PresentationCompletion.Completed, await waiting);
        Assert.Equal(1, app.View.ReadGameState().State.Turn);
    }

    [Fact]
    public async Task ResultInputIsUnavailableUntilFinalResultFrameAndNewWait()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        GameTestSteps.PlayTurn(session, app, GameCommand.Up);
        GameTestSteps.PlayTurn(session, app, GameCommand.Right);
        var result = session.Gameplay.UpdateGameState(id,
            (state, _, _, completed) => WorldRules.BeginResultPresentation(state, completed)).Presentation!.Value;
        Assert.False(app.View.TrySubmitCommand(id, GameCommand.Retry));
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.UpdateGameState(id,
            (state, _, _, completed) => WorldRules.EnableResultInput(state, completed)).Status);
        Assert.False(app.Gameplay.SetPaused(id, true));
        GameTestSteps.Complete(app, result);
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.UpdateGameState(id,
            (state, _, _, completed) => WorldRules.EnableResultInput(state, completed)).Status);
        Assert.False(app.View.TrySubmitCommand(id, GameCommand.Retry));
        var input = session.Gameplay.ReadCommandAsync(id, TestContext.Current.CancellationToken);
        Assert.True(app.View.TrySubmitCommand(id, GameCommand.Retry));
        var retryCommand = await input;
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, retryCommand)).Status);
        Assert.True(session.Gameplay.ReadGameState(id).State.RetryRequested);
        session.Gameplay.EndSession(id);
        using var fresh = GameTestSteps.Ready(app);
        Assert.NotEqual(id, fresh.SessionId);
        Assert.Equal(0, app.View.ReadGameState().State.Turn);
        Assert.Equal(GameOutcome.Running, app.View.ReadGameState().State.Outcome);
        Assert.False(app.View.TryCompletePresentation(result));
    }

    [Fact]
    public async Task PauseRetainsAcceptedCommandUntilResumeAndClosesNewInput()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        var read = session.Gameplay.ReadCommandAsync(id, TestContext.Current.CancellationToken);
        Assert.True(app.View.ReadGameState().Info.WaitingForInput);
        Assert.True(app.View.TrySubmitCommand(id, GameCommand.Up));
        Assert.True(app.Gameplay.SetPaused(id, true));
        var command = await read;
        var before = app.View.ReadGameState();
        Assert.Equal(GameUpdateStatus.Deferred, session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, command)).Status);
        Assert.False(app.View.TrySubmitCommand(id, GameCommand.Right));
        var checkpoint = session.Gameplay.WaitForActiveAsync(id, TestContext.Current.CancellationToken).AsTask();
        Assert.False(checkpoint.IsCompleted);
        Assert.True(app.Gameplay.SetPaused(id, false));
        Assert.True(await checkpoint);
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, command)).Status);
        Assert.Equal(0, app.View.ReadGameState().State.Turn);
        Assert.Equal(before.State.Player, app.View.ReadGameState().State.Player);
        Assert.NotNull(app.View.ReadGameState().State.Selection);
    }

    [Fact]
    public async Task ShutdownWhilePausedReleasesInputPresentationAndCheckpointWaits()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        var input = session.Gameplay.ReadCommandAsync(id, TestContext.Current.CancellationToken).AsTask();
        Assert.True(app.Gameplay.SetPaused(id, true));
        var checkpoint = session.Gameplay.WaitForActiveAsync(id, TestContext.Current.CancellationToken).AsTask();
        app.Lifetime.RequestShutdown();
        Assert.Equal(GameCommand.Stop, await input);
        Assert.False(await checkpoint);
        Assert.True(app.Lifetime.ShutdownRequested);
        Assert.False(app.Gameplay.SetPaused(id, false));
        Assert.Equal(GameUpdateStatus.Stopped, session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyPlayer(state)).Status);
        Assert.False(app.View.TrySubmitCommand(id, GameCommand.Up));
    }

    [Fact]
    public async Task OldPresentationAndControlCannotAffectRetriedSession()
    {
        using var app = new EscapeApplicationContext();
        using var old = GameTestSteps.CreateSessionContext(app);
        var oldKey = old.Gameplay.BeginSession(old.SessionId, WorldRules.BeginSession).Presentation!.Value;
        var stoppedWait = old.Gameplay.WaitForPresentationCompletionAsync(oldKey, TestContext.Current.CancellationToken).AsTask();
        old.Gameplay.EndSession(old.SessionId);
        Assert.Equal(PresentationCompletion.Stopped, await stoppedWait);
        using var fresh = GameTestSteps.CreateSessionContext(app);
        var freshKey = fresh.Gameplay.BeginSession(fresh.SessionId, WorldRules.BeginSession).Presentation!.Value;
        var freshWait = fresh.Gameplay.WaitForPresentationCompletionAsync(freshKey, TestContext.Current.CancellationToken).AsTask();
        Assert.NotEqual(old.SessionId, fresh.SessionId);
        Assert.False(app.View.TryCompletePresentation(oldKey));
        Assert.False(app.Gameplay.RequestSkip(oldKey));
        Assert.False(app.Gameplay.SetPaused(old.SessionId, true));
        Assert.False(app.View.TrySubmitCommand(old.SessionId, GameCommand.Retry));
        old.Gameplay.EndSession(old.SessionId);
        Assert.False(freshWait.IsCompleted);
        Assert.Equal(freshKey, app.View.ReadGameState().Info.Event!.Key);
        GameTestSteps.Complete(app, freshKey);
        Assert.Equal(PresentationCompletion.Completed, await freshWait);
    }

    [Fact]
    public async Task PresentationWaitIsCancelableWithoutRollingBackCommittedMove()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Up));
        session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Confirm));
        var key = session.Gameplay.UpdateGameState(id,
            (state, _, _, _) => WorldRules.ApplyPlayer(state)).Presentation!.Value;
        using var cancellation = new CancellationTokenSource();
        var wait = session.Gameplay.WaitForPresentationCompletionAsync(key, cancellation.Token).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
        Assert.Equal(new GridPosition(1, 2), app.View.ReadGameState().State.Player);
        Assert.Equal(1, app.View.ReadGameState().State.Turn);
        session.Gameplay.EndSession(id);
        Assert.Equal(PresentationCompletion.Stopped,
            await session.Gameplay.WaitForPresentationCompletionAsync(key, TestContext.Current.CancellationToken));
    }
}
