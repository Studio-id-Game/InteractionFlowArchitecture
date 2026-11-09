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
        using var session = DomainTests.Ready(app);
        var id = session.SessionId;
        session.World.ApplySelection(id, GameCommand.Up);
        session.World.ApplySelection(id, GameCommand.Confirm);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.Input.ReadAsync(id, TestContext.Current.CancellationToken).AsTask());

        Assert.False(app.Display.Snapshot().AcceptingInput);
        Assert.True(session.World.GetProgress(id).HasConfirmedAction);
        Assert.Equal(MutationStatus.Applied, session.World.ApplyPlayer(id).Status);
    }

    [Fact]
    public void PauseBeforeCommitPreservesConfirmedActionAndPreventsDoubleApplication()
    {
        using var app = new EscapeApplicationContext();
        using var session = DomainTests.Ready(app);
        var id = session.SessionId;
        session.World.ApplySelection(id, GameCommand.Up);
        session.World.ApplySelection(id, GameCommand.Confirm);
        var token = session.Cancellation.GetToken();
        Assert.True(session.Control.TrySetPaused(id, true));
        Assert.Equal(MutationStatus.Deferred, session.World.ApplyPlayer(id).Status);
        Assert.True(session.World.GetProgress(id).HasConfirmedAction);
        Assert.Equal(0, app.Display.Snapshot().Turn);
        Assert.True(session.Control.TrySetPaused(id, false));
        var applied = session.World.ApplyPlayer(id);
        Assert.Equal(MutationStatus.Applied, applied.Status);
        Assert.Equal(MutationStatus.Rejected, session.World.ApplyPlayer(id).Status);
        Assert.Equal(1, app.Display.Snapshot().Turn);
        Assert.Equal(token, session.Cancellation.GetToken());
        Assert.Equal(id, app.Display.Snapshot().SessionId);
    }

    [Fact]
    public async Task OldPresentationKeyCannotSkipOrCompleteNextPresentationInSameSession()
    {
        using var app = new EscapeApplicationContext();
        using var session = DomainTests.Ready(app);
        var id = session.SessionId;
        session.World.ApplySelection(id, GameCommand.Up);
        session.World.ApplySelection(id, GameCommand.Confirm);
        var player = session.World.ApplyPlayer(id).Presentation!.Value;
        DomainTests.Complete(session, player);
        var enemy = session.World.ApplyEnemy(id).Presentation!.Value;
        var waiting = session.Presentation.WaitAsync(enemy, TestContext.Current.CancellationToken).AsTask();
        Assert.False(session.Presentation.TrySkip(player));
        Assert.False(session.Presentation.TryComplete(player));
        Assert.False(session.Presentation.TryComplete(enemy with { Version = enemy.Version - 1 }));
        Assert.False(app.Display.Snapshot().Presentation!.SkipRequested);
        Assert.False(waiting.IsCompleted);
        Assert.True(session.Presentation.TrySkip(enemy));
        DomainTests.Complete(session, enemy);
        Assert.Equal(PresentationCompletion.Completed, await waiting);
        Assert.Equal(1, app.Display.Snapshot().Turn);
    }

    [Fact]
    public async Task ResultInputIsUnavailableUntilFinalResultFrameAndNewWait()
    {
        using var app = new EscapeApplicationContext();
        using var session = DomainTests.Ready(app);
        var id = session.SessionId;
        DomainTests.PlayTurn(session, GameCommand.Up);
        DomainTests.PlayTurn(session, GameCommand.Right);
        var result = session.World.ShowResult(id).Presentation!.Value;
        Assert.False(session.Input.TrySubmit(id, GameCommand.Retry));
        Assert.Equal(MutationStatus.Rejected, session.World.EnableResultInput(id));
        Assert.False(session.Control.TrySetPaused(id, true));
        DomainTests.Complete(session, result);
        Assert.Equal(MutationStatus.Applied, session.World.EnableResultInput(id));
        Assert.False(session.Input.TrySubmit(id, GameCommand.Retry));
        var input = session.Input.ReadAsync(id, TestContext.Current.CancellationToken);
        Assert.True(session.Input.TrySubmit(id, GameCommand.Retry));
        Assert.Equal(MutationStatus.Applied, session.World.ApplySelection(id, await input));
        Assert.True(session.World.GetProgress(id).RetryRequested);
        session.World.EndSession(id);
        using var fresh = DomainTests.Ready(app);
        Assert.NotEqual(id, fresh.SessionId);
        Assert.Equal(0, app.Display.Snapshot().Turn);
        Assert.Equal(GameOutcome.Running, app.Display.Snapshot().Outcome);
        Assert.False(session.Presentation.TryComplete(result));
    }

    [Fact]
    public async Task PauseRetainsAcceptedCommandUntilResumeAndClosesNewInput()
    {
        using var app = new EscapeApplicationContext();
        using var session = DomainTests.Ready(app);
        var id = session.SessionId;
        var read = session.Input.ReadAsync(id, TestContext.Current.CancellationToken);
        Assert.True(app.Display.Snapshot().AcceptingInput);
        Assert.True(session.Input.TrySubmit(id, GameCommand.Up));
        Assert.True(session.Control.TrySetPaused(id, true));
        var command = await read;
        var before = app.Display.Snapshot();
        Assert.Equal(MutationStatus.Deferred, session.World.ApplySelection(id, command));
        Assert.False(session.Input.TrySubmit(id, GameCommand.Right));
        var checkpoint = session.Control.WaitForActiveAsync(id, TestContext.Current.CancellationToken).AsTask();
        Assert.False(checkpoint.IsCompleted);
        Assert.True(session.Control.TrySetPaused(id, false));
        Assert.True(await checkpoint);
        Assert.Equal(MutationStatus.Applied, session.World.ApplySelection(id, command));
        Assert.Equal(0, app.Display.Snapshot().Turn);
        Assert.Equal(before.Player, app.Display.Snapshot().Player);
        Assert.NotNull(app.Display.Snapshot().Selection);
    }

    [Fact]
    public async Task PresentationClockAndAcknowledgmentFreezeAcrossPauseAndResumeRevision()
    {
        using var app = new EscapeApplicationContext();
        using var session = DomainTests.Ready(app);
        var id = session.SessionId;
        session.World.ApplySelection(id, GameCommand.Up);
        session.World.ApplySelection(id, GameCommand.Confirm);
        var key = session.World.ApplyPlayer(id).Presentation!.Value;
        var revision = app.Display.Snapshot().ControlRevision;
        session.Presentation.Advance(key, 10, revision);
        Assert.Equal(0, app.Display.Snapshot().Presentation!.ElapsedSeconds);
        session.Presentation.Advance(key, .05, revision);
        Assert.True(session.Control.TrySetPaused(id, true));
        Assert.Equal(.05, session.Presentation.Advance(key, 99, revision)!.ElapsedSeconds);
        Assert.False(session.Presentation.TryComplete(key));
        Assert.False(session.Presentation.TrySkip(key));
        var wait = session.Presentation.WaitAsync(key, TestContext.Current.CancellationToken).AsTask();
        Assert.False(wait.IsCompleted);
        Assert.True(session.Control.TrySetPaused(id, false));
        Assert.Equal(.05, session.Presentation.Advance(key, 99, revision)!.ElapsedSeconds);
        revision = app.Display.Snapshot().ControlRevision;
        session.Presentation.Advance(key, 1, revision);
        Assert.True(session.Presentation.TryComplete(key));
        Assert.False(session.Presentation.TryComplete(key));
        Assert.Equal(PresentationCompletion.Completed, await wait);
        Assert.Equal(1, app.Display.Snapshot().Turn);
    }

    [Fact]
    public async Task ShutdownWhilePausedReleasesInputPresentationAndCheckpointWaits()
    {
        using var app = new EscapeApplicationContext();
        using var session = DomainTests.Ready(app);
        var id = session.SessionId;
        var input = session.Input.ReadAsync(id, TestContext.Current.CancellationToken).AsTask();
        Assert.True(session.Control.TrySetPaused(id, true));
        var checkpoint = session.Control.WaitForActiveAsync(id, TestContext.Current.CancellationToken).AsTask();
        app.World.RequestShutdown();
        Assert.Equal(GameCommand.Stop, await input);
        Assert.False(await checkpoint);
        Assert.True(app.World.ShutdownRequested);
        Assert.False(session.Control.TrySetPaused(id, false));
        Assert.Equal(MutationStatus.Stopped, session.World.ApplyPlayer(id).Status);
        Assert.False(session.Input.TrySubmit(id, GameCommand.Up));
    }

    [Fact]
    public async Task OldPresentationAndControlCannotAffectRetriedSession()
    {
        using var app = new EscapeApplicationContext();
        using var old = app.CreateSessionContext();
        var oldKey = old.World.BeginSession(old.SessionId).Presentation!.Value;
        var stoppedWait = old.Presentation.WaitAsync(oldKey, TestContext.Current.CancellationToken).AsTask();
        old.World.EndSession(old.SessionId);
        Assert.Equal(PresentationCompletion.Stopped, await stoppedWait);
        using var fresh = app.CreateSessionContext();
        var freshKey = fresh.World.BeginSession(fresh.SessionId).Presentation!.Value;
        var freshWait = fresh.Presentation.WaitAsync(freshKey, TestContext.Current.CancellationToken).AsTask();
        Assert.NotEqual(old.SessionId, fresh.SessionId);
        Assert.False(old.Presentation.TryComplete(oldKey));
        Assert.False(old.Presentation.TrySkip(oldKey));
        Assert.False(old.Control.TrySetPaused(old.SessionId, true));
        Assert.False(old.Input.TrySubmit(old.SessionId, GameCommand.Retry));
        old.World.EndSession(old.SessionId);
        Assert.False(freshWait.IsCompleted);
        Assert.Equal(freshKey, app.Display.Snapshot().Presentation!.Key);
        DomainTests.Complete(fresh, freshKey);
        Assert.Equal(PresentationCompletion.Completed, await freshWait);
    }

    [Fact]
    public async Task PresentationWaitIsCancelableWithoutRollingBackCommittedMove()
    {
        using var app = new EscapeApplicationContext();
        using var session = DomainTests.Ready(app);
        var id = session.SessionId;
        session.World.ApplySelection(id, GameCommand.Up);
        session.World.ApplySelection(id, GameCommand.Confirm);
        var key = session.World.ApplyPlayer(id).Presentation!.Value;
        using var cancellation = new CancellationTokenSource();
        var wait = session.Presentation.WaitAsync(key, cancellation.Token).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
        Assert.Equal(new GridPosition(1, 2), app.Display.Snapshot().Player);
        Assert.Equal(1, app.Display.Snapshot().Turn);
        session.World.EndSession(id);
        Assert.Equal(PresentationCompletion.Stopped,
            await session.Presentation.WaitAsync(key, TestContext.Current.CancellationToken));
    }
}
