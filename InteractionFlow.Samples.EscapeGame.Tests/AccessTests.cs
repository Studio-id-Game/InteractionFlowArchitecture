using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class AccessTests
{
    [Fact]
    public async Task ConfirmedActionCannotStartAnotherInputWait()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.ApplySelection(id, GameCommand.Up);
        session.Gameplay.ApplySelection(id, GameCommand.Confirm);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.Gameplay.ReadAsync(id, TestContext.Current.CancellationToken).AsTask());

        Assert.False(app.View.Snapshot().AcceptingInput);
        Assert.True(session.Gameplay.GetProgress(id).HasConfirmedAction);
        Assert.Equal(MutationStatus.Applied, session.Gameplay.ApplyPlayer(id).Status);
    }

    [Fact]
    public void PauseBeforeCommitPreservesConfirmedActionAndPreventsDoubleApplication()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.ApplySelection(id, GameCommand.Up);
        session.Gameplay.ApplySelection(id, GameCommand.Confirm);
        var token = session.Cancellation.GetToken();
        Assert.True(app.View.TrySetPaused(id, true));
        Assert.Equal(MutationStatus.Deferred, session.Gameplay.ApplyPlayer(id).Status);
        Assert.True(session.Gameplay.GetProgress(id).HasConfirmedAction);
        Assert.Equal(0, app.View.Snapshot().Turn);
        Assert.True(app.View.TrySetPaused(id, false));
        var applied = session.Gameplay.ApplyPlayer(id);
        Assert.Equal(MutationStatus.Applied, applied.Status);
        Assert.Equal(MutationStatus.Rejected, session.Gameplay.ApplyPlayer(id).Status);
        Assert.Equal(1, app.View.Snapshot().Turn);
        Assert.Equal(token, session.Cancellation.GetToken());
        Assert.Equal(id, app.View.Snapshot().SessionId);
    }

    [Fact]
    public async Task OldPresentationKeyCannotSkipOrCompleteNextPresentationInSameSession()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.ApplySelection(id, GameCommand.Up);
        session.Gameplay.ApplySelection(id, GameCommand.Confirm);
        var player = session.Gameplay.ApplyPlayer(id).Presentation!.Value;
        GameTestSteps.Complete(app, player);
        var enemy = session.Gameplay.ApplyEnemy(id).Presentation!.Value;
        var waiting = session.Gameplay.WaitForPresentationAsync(enemy, TestContext.Current.CancellationToken).AsTask();
        Assert.False(app.View.TrySkip(player));
        Assert.False(app.View.TryComplete(player));
        Assert.False(app.View.TryComplete(enemy with { Version = enemy.Version - 1 }));
        Assert.False(app.View.Snapshot().Presentation!.SkipRequested);
        Assert.False(waiting.IsCompleted);
        Assert.True(app.View.TrySkip(enemy));
        GameTestSteps.Complete(app, enemy);
        Assert.Equal(PresentationCompletion.Completed, await waiting);
        Assert.Equal(1, app.View.Snapshot().Turn);
    }

    [Fact]
    public async Task ResultInputIsUnavailableUntilFinalResultFrameAndNewWait()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        GameTestSteps.PlayTurn(session, app, GameCommand.Up);
        GameTestSteps.PlayTurn(session, app, GameCommand.Right);
        var result = session.Gameplay.ShowResult(id).Presentation!.Value;
        Assert.False(app.View.TrySubmit(id, GameCommand.Retry));
        Assert.Equal(MutationStatus.Rejected, session.Gameplay.EnableResultInput(id));
        Assert.False(app.View.TrySetPaused(id, true));
        GameTestSteps.Complete(app, result);
        Assert.Equal(MutationStatus.Applied, session.Gameplay.EnableResultInput(id));
        Assert.False(app.View.TrySubmit(id, GameCommand.Retry));
        var input = session.Gameplay.ReadAsync(id, TestContext.Current.CancellationToken);
        Assert.True(app.View.TrySubmit(id, GameCommand.Retry));
        Assert.Equal(MutationStatus.Applied, session.Gameplay.ApplySelection(id, await input));
        Assert.True(session.Gameplay.GetProgress(id).RetryRequested);
        session.Gameplay.EndSession(id);
        using var fresh = GameTestSteps.Ready(app);
        Assert.NotEqual(id, fresh.SessionId);
        Assert.Equal(0, app.View.Snapshot().Turn);
        Assert.Equal(GameOutcome.Running, app.View.Snapshot().Outcome);
        Assert.False(app.View.TryComplete(result));
    }

    [Fact]
    public async Task PauseRetainsAcceptedCommandUntilResumeAndClosesNewInput()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        var read = session.Gameplay.ReadAsync(id, TestContext.Current.CancellationToken);
        Assert.True(app.View.Snapshot().AcceptingInput);
        Assert.True(app.View.TrySubmit(id, GameCommand.Up));
        Assert.True(app.View.TrySetPaused(id, true));
        var command = await read;
        var before = app.View.Snapshot();
        Assert.Equal(MutationStatus.Deferred, session.Gameplay.ApplySelection(id, command));
        Assert.False(app.View.TrySubmit(id, GameCommand.Right));
        var checkpoint = session.Gameplay.WaitForActiveAsync(id, TestContext.Current.CancellationToken).AsTask();
        Assert.False(checkpoint.IsCompleted);
        Assert.True(app.View.TrySetPaused(id, false));
        Assert.True(await checkpoint);
        Assert.Equal(MutationStatus.Applied, session.Gameplay.ApplySelection(id, command));
        Assert.Equal(0, app.View.Snapshot().Turn);
        Assert.Equal(before.Player, app.View.Snapshot().Player);
        Assert.NotNull(app.View.Snapshot().Selection);
    }

    [Fact]
    public async Task PresentationClockAndAcknowledgmentFreezeAcrossPauseAndResumeRevision()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.ApplySelection(id, GameCommand.Up);
        session.Gameplay.ApplySelection(id, GameCommand.Confirm);
        var key = session.Gameplay.ApplyPlayer(id).Presentation!.Value;
        var revision = app.View.Snapshot().ControlRevision;
        app.View.Advance(key, 10, revision);
        Assert.Equal(0, app.View.Snapshot().Presentation!.ElapsedSeconds);
        app.View.Advance(key, .05, revision);
        Assert.True(app.View.TrySetPaused(id, true));
        Assert.Equal(.05, app.View.Advance(key, 99, revision)!.ElapsedSeconds);
        Assert.False(app.View.TryComplete(key));
        Assert.False(app.View.TrySkip(key));
        var wait = session.Gameplay.WaitForPresentationAsync(key, TestContext.Current.CancellationToken).AsTask();
        Assert.False(wait.IsCompleted);
        Assert.True(app.View.TrySetPaused(id, false));
        Assert.Equal(.05, app.View.Advance(key, 99, revision)!.ElapsedSeconds);
        revision = app.View.Snapshot().ControlRevision;
        app.View.Advance(key, 1, revision);
        Assert.True(app.View.TryComplete(key));
        Assert.False(app.View.TryComplete(key));
        Assert.Equal(PresentationCompletion.Completed, await wait);
        Assert.Equal(1, app.View.Snapshot().Turn);
    }

    [Fact]
    public async Task ShutdownWhilePausedReleasesInputPresentationAndCheckpointWaits()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        var input = session.Gameplay.ReadAsync(id, TestContext.Current.CancellationToken).AsTask();
        Assert.True(app.View.TrySetPaused(id, true));
        var checkpoint = session.Gameplay.WaitForActiveAsync(id, TestContext.Current.CancellationToken).AsTask();
        app.Lifetime.RequestShutdown();
        Assert.Equal(GameCommand.Stop, await input);
        Assert.False(await checkpoint);
        Assert.True(app.Lifetime.ShutdownRequested);
        Assert.False(app.View.TrySetPaused(id, false));
        Assert.Equal(MutationStatus.Stopped, session.Gameplay.ApplyPlayer(id).Status);
        Assert.False(app.View.TrySubmit(id, GameCommand.Up));
    }

    [Fact]
    public async Task OldPresentationAndControlCannotAffectRetriedSession()
    {
        using var app = new EscapeApplicationContext();
        using var old = GameTestSteps.CreateSession(app);
        var oldKey = old.Gameplay.BeginSession(old.SessionId).Presentation!.Value;
        var stoppedWait = old.Gameplay.WaitForPresentationAsync(oldKey, TestContext.Current.CancellationToken).AsTask();
        old.Gameplay.EndSession(old.SessionId);
        Assert.Equal(PresentationCompletion.Stopped, await stoppedWait);
        using var fresh = GameTestSteps.CreateSession(app);
        var freshKey = fresh.Gameplay.BeginSession(fresh.SessionId).Presentation!.Value;
        var freshWait = fresh.Gameplay.WaitForPresentationAsync(freshKey, TestContext.Current.CancellationToken).AsTask();
        Assert.NotEqual(old.SessionId, fresh.SessionId);
        Assert.False(app.View.TryComplete(oldKey));
        Assert.False(app.View.TrySkip(oldKey));
        Assert.False(app.View.TrySetPaused(old.SessionId, true));
        Assert.False(app.View.TrySubmit(old.SessionId, GameCommand.Retry));
        old.Gameplay.EndSession(old.SessionId);
        Assert.False(freshWait.IsCompleted);
        Assert.Equal(freshKey, app.View.Snapshot().Presentation!.Key);
        GameTestSteps.Complete(app, freshKey);
        Assert.Equal(PresentationCompletion.Completed, await freshWait);
    }

    [Fact]
    public async Task PresentationWaitIsCancelableWithoutRollingBackCommittedMove()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.ApplySelection(id, GameCommand.Up);
        session.Gameplay.ApplySelection(id, GameCommand.Confirm);
        var key = session.Gameplay.ApplyPlayer(id).Presentation!.Value;
        using var cancellation = new CancellationTokenSource();
        var wait = session.Gameplay.WaitForPresentationAsync(key, cancellation.Token).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
        Assert.Equal(new GridPosition(1, 2), app.View.Snapshot().Player);
        Assert.Equal(1, app.View.Snapshot().Turn);
        session.Gameplay.EndSession(id);
        Assert.Equal(PresentationCompletion.Stopped,
            await session.Gameplay.WaitForPresentationAsync(key, TestContext.Current.CancellationToken));
    }
}
