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
        session.Gameplay.ApplyInputCommand(id, GameCommand.Up);
        session.Gameplay.ApplyInputCommand(id, GameCommand.Confirm);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.Gameplay.ReadCommandAsync(id, TestContext.Current.CancellationToken).AsTask());

        Assert.False(app.View.CaptureSnapshot().AcceptingInput);
        Assert.True(session.Gameplay.GetSessionProgress(id).HasConfirmedAction);
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.ApplyPlayerAction(id).Status);
    }

    [Fact]
    public void PauseBeforeCommitPreservesConfirmedActionAndPreventsDoubleApplication()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.ApplyInputCommand(id, GameCommand.Up);
        session.Gameplay.ApplyInputCommand(id, GameCommand.Confirm);
        var token = session.Cancellation.GetToken();
        Assert.True(app.View.TrySetPaused(id, true));
        Assert.Equal(GameUpdateStatus.Deferred, session.Gameplay.ApplyPlayerAction(id).Status);
        Assert.True(session.Gameplay.GetSessionProgress(id).HasConfirmedAction);
        Assert.Equal(0, app.View.CaptureSnapshot().Turn);
        Assert.True(app.View.TrySetPaused(id, false));
        var applied = session.Gameplay.ApplyPlayerAction(id);
        Assert.Equal(GameUpdateStatus.Applied, applied.Status);
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.ApplyPlayerAction(id).Status);
        Assert.Equal(1, app.View.CaptureSnapshot().Turn);
        Assert.Equal(token, session.Cancellation.GetToken());
        Assert.Equal(id, app.View.CaptureSnapshot().SessionId);
    }

    [Fact]
    public async Task OldPresentationKeyCannotSkipOrCompleteNextPresentationInSameSession()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.ApplyInputCommand(id, GameCommand.Up);
        session.Gameplay.ApplyInputCommand(id, GameCommand.Confirm);
        var player = session.Gameplay.ApplyPlayerAction(id).Presentation!.Value;
        GameTestSteps.Complete(app, player);
        var enemy = session.Gameplay.ApplyEnemyAction(id).Presentation!.Value;
        var waiting = session.Gameplay.WaitForPresentationCompletionAsync(enemy, TestContext.Current.CancellationToken).AsTask();
        Assert.False(app.View.TryRequestPresentationSkip(player));
        Assert.False(app.View.TryCompletePresentation(player));
        Assert.False(app.View.TryCompletePresentation(enemy with { Version = enemy.Version - 1 }));
        Assert.False(app.View.CaptureSnapshot().Presentation!.SkipRequested);
        Assert.False(waiting.IsCompleted);
        Assert.True(app.View.TryRequestPresentationSkip(enemy));
        GameTestSteps.Complete(app, enemy);
        Assert.Equal(PresentationCompletion.Completed, await waiting);
        Assert.Equal(1, app.View.CaptureSnapshot().Turn);
    }

    [Fact]
    public async Task ResultInputIsUnavailableUntilFinalResultFrameAndNewWait()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        GameTestSteps.PlayTurn(session, app, GameCommand.Up);
        GameTestSteps.PlayTurn(session, app, GameCommand.Right);
        var result = session.Gameplay.BeginResultPresentation(id).Presentation!.Value;
        Assert.False(app.View.TrySubmitCommand(id, GameCommand.Retry));
        Assert.Equal(GameUpdateStatus.Rejected, session.Gameplay.EnableResultInput(id));
        Assert.False(app.View.TrySetPaused(id, true));
        GameTestSteps.Complete(app, result);
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.EnableResultInput(id));
        Assert.False(app.View.TrySubmitCommand(id, GameCommand.Retry));
        var input = session.Gameplay.ReadCommandAsync(id, TestContext.Current.CancellationToken);
        Assert.True(app.View.TrySubmitCommand(id, GameCommand.Retry));
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.ApplyInputCommand(id, await input));
        Assert.True(session.Gameplay.GetSessionProgress(id).RetryRequested);
        session.Gameplay.EndSession(id);
        using var fresh = GameTestSteps.Ready(app);
        Assert.NotEqual(id, fresh.SessionId);
        Assert.Equal(0, app.View.CaptureSnapshot().Turn);
        Assert.Equal(GameOutcome.Running, app.View.CaptureSnapshot().Outcome);
        Assert.False(app.View.TryCompletePresentation(result));
    }

    [Fact]
    public async Task PauseRetainsAcceptedCommandUntilResumeAndClosesNewInput()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        var read = session.Gameplay.ReadCommandAsync(id, TestContext.Current.CancellationToken);
        Assert.True(app.View.CaptureSnapshot().AcceptingInput);
        Assert.True(app.View.TrySubmitCommand(id, GameCommand.Up));
        Assert.True(app.View.TrySetPaused(id, true));
        var command = await read;
        var before = app.View.CaptureSnapshot();
        Assert.Equal(GameUpdateStatus.Deferred, session.Gameplay.ApplyInputCommand(id, command));
        Assert.False(app.View.TrySubmitCommand(id, GameCommand.Right));
        var checkpoint = session.Gameplay.WaitForActiveAsync(id, TestContext.Current.CancellationToken).AsTask();
        Assert.False(checkpoint.IsCompleted);
        Assert.True(app.View.TrySetPaused(id, false));
        Assert.True(await checkpoint);
        Assert.Equal(GameUpdateStatus.Applied, session.Gameplay.ApplyInputCommand(id, command));
        Assert.Equal(0, app.View.CaptureSnapshot().Turn);
        Assert.Equal(before.Player, app.View.CaptureSnapshot().Player);
        Assert.NotNull(app.View.CaptureSnapshot().Selection);
    }

    [Fact]
    public async Task PresentationClockAndAcknowledgmentFreezeAcrossPauseAndResumeRevision()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.ApplyInputCommand(id, GameCommand.Up);
        session.Gameplay.ApplyInputCommand(id, GameCommand.Confirm);
        var key = session.Gameplay.ApplyPlayerAction(id).Presentation!.Value;
        var revision = app.View.CaptureSnapshot().ControlRevision;
        app.View.AdvancePresentation(key, 10, revision);
        Assert.Equal(0, app.View.CaptureSnapshot().Presentation!.ElapsedSeconds);
        app.View.AdvancePresentation(key, .05, revision);
        Assert.True(app.View.TrySetPaused(id, true));
        Assert.Equal(.05, app.View.AdvancePresentation(key, 99, revision)!.ElapsedSeconds);
        Assert.False(app.View.TryCompletePresentation(key));
        Assert.False(app.View.TryRequestPresentationSkip(key));
        var wait = session.Gameplay.WaitForPresentationCompletionAsync(key, TestContext.Current.CancellationToken).AsTask();
        Assert.False(wait.IsCompleted);
        Assert.True(app.View.TrySetPaused(id, false));
        Assert.Equal(.05, app.View.AdvancePresentation(key, 99, revision)!.ElapsedSeconds);
        revision = app.View.CaptureSnapshot().ControlRevision;
        app.View.AdvancePresentation(key, 1, revision);
        Assert.True(app.View.TryCompletePresentation(key));
        Assert.False(app.View.TryCompletePresentation(key));
        Assert.Equal(PresentationCompletion.Completed, await wait);
        Assert.Equal(1, app.View.CaptureSnapshot().Turn);
    }

    [Fact]
    public async Task ShutdownWhilePausedReleasesInputPresentationAndCheckpointWaits()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        var input = session.Gameplay.ReadCommandAsync(id, TestContext.Current.CancellationToken).AsTask();
        Assert.True(app.View.TrySetPaused(id, true));
        var checkpoint = session.Gameplay.WaitForActiveAsync(id, TestContext.Current.CancellationToken).AsTask();
        app.Lifetime.RequestShutdown();
        Assert.Equal(GameCommand.Stop, await input);
        Assert.False(await checkpoint);
        Assert.True(app.Lifetime.ShutdownRequested);
        Assert.False(app.View.TrySetPaused(id, false));
        Assert.Equal(GameUpdateStatus.Stopped, session.Gameplay.ApplyPlayerAction(id).Status);
        Assert.False(app.View.TrySubmitCommand(id, GameCommand.Up));
    }

    [Fact]
    public async Task OldPresentationAndControlCannotAffectRetriedSession()
    {
        using var app = new EscapeApplicationContext();
        using var old = GameTestSteps.CreateSessionContext(app);
        var oldKey = old.Gameplay.BeginSession(old.SessionId).Presentation!.Value;
        var stoppedWait = old.Gameplay.WaitForPresentationCompletionAsync(oldKey, TestContext.Current.CancellationToken).AsTask();
        old.Gameplay.EndSession(old.SessionId);
        Assert.Equal(PresentationCompletion.Stopped, await stoppedWait);
        using var fresh = GameTestSteps.CreateSessionContext(app);
        var freshKey = fresh.Gameplay.BeginSession(fresh.SessionId).Presentation!.Value;
        var freshWait = fresh.Gameplay.WaitForPresentationCompletionAsync(freshKey, TestContext.Current.CancellationToken).AsTask();
        Assert.NotEqual(old.SessionId, fresh.SessionId);
        Assert.False(app.View.TryCompletePresentation(oldKey));
        Assert.False(app.View.TryRequestPresentationSkip(oldKey));
        Assert.False(app.View.TrySetPaused(old.SessionId, true));
        Assert.False(app.View.TrySubmitCommand(old.SessionId, GameCommand.Retry));
        old.Gameplay.EndSession(old.SessionId);
        Assert.False(freshWait.IsCompleted);
        Assert.Equal(freshKey, app.View.CaptureSnapshot().Presentation!.Key);
        GameTestSteps.Complete(app, freshKey);
        Assert.Equal(PresentationCompletion.Completed, await freshWait);
    }

    [Fact]
    public async Task PresentationWaitIsCancelableWithoutRollingBackCommittedMove()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        session.Gameplay.ApplyInputCommand(id, GameCommand.Up);
        session.Gameplay.ApplyInputCommand(id, GameCommand.Confirm);
        var key = session.Gameplay.ApplyPlayerAction(id).Presentation!.Value;
        using var cancellation = new CancellationTokenSource();
        var wait = session.Gameplay.WaitForPresentationCompletionAsync(key, cancellation.Token).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
        Assert.Equal(new GridPosition(1, 2), app.View.CaptureSnapshot().Player);
        Assert.Equal(1, app.View.CaptureSnapshot().Turn);
        session.Gameplay.EndSession(id);
        Assert.Equal(PresentationCompletion.Stopped,
            await session.Gameplay.WaitForPresentationCompletionAsync(key, TestContext.Current.CancellationToken));
    }
}
