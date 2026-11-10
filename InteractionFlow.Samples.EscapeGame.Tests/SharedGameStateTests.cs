using InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class SharedGameStateTests
{
    [Fact]
    public async Task SessionSnapshotsStayStableWithoutReplacingTheGameStateForInputOrPause()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var before = app.View.ReadGameState();
        var input = app.Gameplay.ReadCommandAsync(session.SessionId, TestContext.Current.CancellationToken);
        var waiting = app.View.ReadGameState();

        Assert.Same(before.State, waiting.State);
        Assert.False(before.Info.WaitingForInput);
        Assert.True(waiting.Info.WaitingForInput);
        Assert.Same(waiting, app.View.ReadGameState());

        Assert.True(app.Gameplay.SetPaused(session.SessionId, true));
        var paused = app.View.ReadGameState();
        Assert.Same(before.State, paused.State);
        Assert.Equal(ExecutionMode.Active, waiting.Info.Mode);
        Assert.Equal(ExecutionMode.Paused, paused.Info.Mode);
        Assert.Equal(waiting.Info.ControlRevision + 1, paused.Info.ControlRevision);

        Assert.True(app.Gameplay.SetPaused(session.SessionId, false));
        Assert.Equal(paused.Info.ControlRevision + 1, app.View.ReadGameState().Info.ControlRevision);
        Assert.True(app.View.TrySubmitCommand(session.SessionId, GameCommand.Up));
        Assert.Equal(GameCommand.Up, await input);
        Assert.True(waiting.Info.WaitingForInput);
        Assert.False(app.View.ReadGameState().Info.WaitingForInput);
    }

    [Fact]
    public void FeedbackInstructionsKeepReplaceAndClearSessionInformation()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var before = app.View.ReadGameState();

        app.Gameplay.UpdateGameState(session.SessionId, (state, _, _, _) => new(state, new(false)
        {
            ReplaceFeedback = true,
            Feedback = InputFeedback.InvalidSelection
        }));
        var rejected = app.View.ReadGameState();
        Assert.Same(before.State, rejected.State);
        Assert.Null(before.Info.Feedback);
        Assert.Equal(InputFeedback.InvalidSelection, rejected.Info.Feedback);

        app.Gameplay.UpdateGameState(session.SessionId, (state, _, _, _) => new(state, new(true)));
        Assert.Equal(InputFeedback.InvalidSelection, app.View.ReadGameState().Info.Feedback);
        app.Gameplay.UpdateGameState(session.SessionId, (state, _, _, _) =>
            new(state, new(true) { ReplaceFeedback = true }));
        Assert.Null(app.View.ReadGameState().Info.Feedback);
        Assert.Equal(InputFeedback.InvalidSelection, rejected.Info.Feedback);
    }

    [Fact]
    public void RejectedRuleStillPublishesFeedbackWhileDeferredUpdateDoesNotRunTheRule()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var before = app.View.ReadGameState();
        var change = WorldRules.ApplyInputCommand(before.State, GameCommand.Left);

        Assert.False(change.Info.Accepted);
        Assert.Equal(InputFeedback.InvalidSelection, change.Info.Feedback);
        Assert.Null(before.Info.Feedback);

        var result = app.Gameplay.UpdateGameState(session.SessionId, (_, _, _, _) => change);
        Assert.Equal(GameUpdateStatus.Rejected, result.Status);
        Assert.Equal(change.Info.Feedback, app.View.ReadGameState().Info.Feedback);
        Assert.Equal(before.Info.Event?.Key, result.Presentation);

        Assert.True(app.Gameplay.SetPaused(session.SessionId, true));
        var paused = app.View.ReadGameState();
        var deferred = app.Gameplay.UpdateGameState(session.SessionId,
            (_, _, _, _) => throw new InvalidOperationException("A deferred update must not evaluate the rule."));
        Assert.Equal(GameUpdateStatus.Deferred, deferred.Status);
        Assert.Null(deferred.Presentation);
        Assert.Same(paused, app.View.ReadGameState());
    }

    [Fact]
    public void RejectedInitializationDoesNotPublishAndCanBeRetried()
    {
        using var app = new EscapeApplicationContext();
        var id = app.Gameplay.AllocateSession();
        var before = app.View.ReadGameState();

        var rejected = app.Gameplay.BeginSession(id, state => new(state with { Turn = 123 }, new(false)));

        Assert.Equal(GameUpdateStatus.Rejected, rejected.Status);
        Assert.Same(before, app.View.ReadGameState());
        var applied = app.Gameplay.BeginSession(id, WorldRules.BeginSession);
        Assert.Equal(GameUpdateStatus.Applied, applied.Status);
        Assert.Equal(0, app.View.ReadGameState().State.Turn);
        Assert.Equal(app.View.ReadGameState().Info.Event!.Key, applied.Presentation);
    }

    [Fact]
    public async Task ASecondInputWaitCannotReplaceTheCurrentReceiver()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        var first = app.Gameplay.ReadCommandAsync(id, TestContext.Current.CancellationToken).AsTask();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => app.Gameplay.ReadCommandAsync(id, TestContext.Current.CancellationToken).AsTask());

        Assert.False(first.IsCompleted);
        Assert.True(app.View.ReadGameState().Info.WaitingForInput);
        Assert.True(app.View.TrySubmitCommand(id, GameCommand.Up));
        Assert.Equal(GameCommand.Up, await first);
        Assert.False(app.View.ReadGameState().Info.WaitingForInput);

        var next = app.Gameplay.ReadCommandAsync(id, TestContext.Current.CancellationToken).AsTask();
        Assert.True(app.View.TrySubmitCommand(id, GameCommand.Wait));
        Assert.Equal(GameCommand.Wait, await next);
        Assert.False(app.View.ReadGameState().Info.WaitingForInput);
    }

    [Fact]
    public void PreviouslyReadStateIsStableAndFailedUpdateDoesNotPublishAnything()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var before = app.View.ReadGameState();
        var failure = new InvalidOperationException("update failed");
        Assert.Throws<InvalidOperationException>(() => app.Gameplay.UpdateGameState(session.SessionId,
            (state, _, _, _) =>
        {
            var changed = state with { Turn = 123 };
            Assert.Equal(123, changed.Turn);
            throw failure;
        }));
        Assert.Same(before, app.View.ReadGameState());
        app.Gameplay.UpdateGameState(session.SessionId,
            (state, _, _, _) => WorldRules.ApplyInputCommand(state, GameCommand.Up));
        var after = app.View.ReadGameState();
        Assert.Null(before.State.Selection);
        Assert.NotNull(after.State.Selection);
        Assert.NotSame(before, after);
    }

    [Fact]
    public async Task ConcurrentUpdatesDoNotLoseChanges()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
                Assert.Equal(GameUpdateStatus.Applied, app.Gameplay.UpdateGameState(session.SessionId,
                    (state, _, _, _) => new(state with { Turn = state.Turn + 1 }, new(true))).Status);
        }, TestContext.Current.CancellationToken)));
        Assert.Equal(800, app.View.ReadGameState().State.Turn);
    }

    [Fact]
    public async Task AccessTransportsInputAndGameplayDecidesItsMeaning()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        Assert.False(app.View.TrySubmitCommand(id, GameCommand.Up));
        var pending = app.Gameplay.ReadCommandAsync(id, TestContext.Current.CancellationToken);
        Assert.True(app.View.TrySubmitCommand(id, GameCommand.Retry));
        Assert.False(app.View.TrySubmitCommand(id, GameCommand.Up));
        var command = await pending;
        Assert.Equal(GameCommand.Retry, command);
        Assert.Equal(GameUpdateStatus.Rejected,
            app.Gameplay.UpdateGameState(id, (state, _, _, _) => WorldRules.ApplyInputCommand(state, command)).Status);
        Assert.False(app.View.ReadGameState().State.RetryRequested);
        Assert.False(app.View.ReadGameState().Info.WaitingForInput);
    }

    [Fact]
    public async Task CanceledReceiverDoesNotConsumeTheNextInput()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        using var canceled = new CancellationTokenSource();
        var old = app.Gameplay.ReadCommandAsync(session.SessionId, canceled.Token).AsTask();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => old);
        Assert.False(app.View.TrySubmitCommand(session.SessionId, GameCommand.Up));
        var next = app.Gameplay.ReadCommandAsync(session.SessionId, TestContext.Current.CancellationToken);
        Assert.True(app.View.TrySubmitCommand(session.SessionId, GameCommand.Wait));
        Assert.Equal(GameCommand.Wait, await next);
    }

    [Fact]
    public void AnUpdateCannotPublishAnotherSessionsState()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var before = app.View.ReadGameState();
        Assert.Throws<InvalidOperationException>(() => app.Gameplay.UpdateGameState(session.SessionId,
            (state, _, _, _) => new(state with { SessionId = state.SessionId + 1 }, new(true))));
        Assert.Same(before, app.View.ReadGameState());
    }
}
