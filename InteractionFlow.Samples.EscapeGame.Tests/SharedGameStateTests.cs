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
    public void RejectedRuleStillPublishesFeedbackWhileDeferredUpdateDoesNotRunTheRule()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var before = app.View.ReadGameState();
        var change = WorldRules.ApplyInputCommand(before, GameCommand.Left);

        Assert.False(change.Accepted);
        Assert.Equal(InputFeedback.InvalidSelection, change.State.Feedback);
        Assert.Null(before.Feedback);

        var result = app.Gameplay.UpdateGameState(session.SessionId, _ => change);
        Assert.Equal(GameUpdateStatus.Rejected, result.Status);
        Assert.Equal(change.State.Feedback, app.View.ReadGameState().Feedback);
        Assert.Equal(change.State.Event?.Key, result.Presentation);

        Assert.True(app.Gameplay.SetPaused(session.SessionId, true));
        var paused = app.View.ReadGameState();
        var deferred = app.Gameplay.UpdateGameState(session.SessionId,
            _ => throw new InvalidOperationException("A deferred update must not evaluate the rule."));
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

        var rejected = app.Gameplay.BeginSession(id, state => new(false, state with { Turn = 123 }));

        Assert.Equal(GameUpdateStatus.Rejected, rejected.Status);
        Assert.Same(before, app.View.ReadGameState());
        var applied = app.Gameplay.BeginSession(id, WorldRules.BeginSession);
        Assert.Equal(GameUpdateStatus.Applied, applied.Status);
        Assert.Equal(0, app.View.ReadGameState().Turn);
        Assert.Equal(app.View.ReadGameState().Event!.Key, applied.Presentation);
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
        Assert.True(app.View.ReadGameState().WaitingForInput);
        Assert.True(app.View.TrySubmitCommand(id, GameCommand.Up));
        Assert.Equal(GameCommand.Up, await first);
        Assert.False(app.View.ReadGameState().WaitingForInput);

        var next = app.Gameplay.ReadCommandAsync(id, TestContext.Current.CancellationToken).AsTask();
        Assert.True(app.View.TrySubmitCommand(id, GameCommand.Wait));
        Assert.Equal(GameCommand.Wait, await next);
        Assert.False(app.View.ReadGameState().WaitingForInput);
    }

    [Fact]
    public void PreviouslyReadStateIsStableAndFailedUpdateDoesNotPublishAnything()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var before = app.View.ReadGameState();
        var failure = new InvalidOperationException("update failed");
        Assert.Throws<InvalidOperationException>(() => app.Gameplay.UpdateGameState(session.SessionId, state =>
        {
            var changed = state with { Turn = 123 };
            Assert.Equal(123, changed.Turn);
            throw failure;
        }));
        Assert.Same(before, app.View.ReadGameState());
        app.Gameplay.UpdateGameState(session.SessionId, state => WorldRules.ApplyInputCommand(state, GameCommand.Up));
        var after = app.View.ReadGameState();
        Assert.Null(before.Selection);
        Assert.NotNull(after.Selection);
        Assert.NotSame(before, after);
    }

    [Fact]
    public async Task ConcurrentUpdatesDoNotLoseChanges()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var initialVersion = app.View.ReadGameState().Version;
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
                Assert.Equal(GameUpdateStatus.Applied, app.Gameplay.UpdateGameState(session.SessionId,
                    state => new(true, state with { Turn = state.Turn + 1 })).Status);
        }, TestContext.Current.CancellationToken)));
        Assert.Equal(800, app.View.ReadGameState().Turn);
        Assert.Equal(initialVersion + 800, app.View.ReadGameState().Version);
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
            app.Gameplay.UpdateGameState(id, state => WorldRules.ApplyInputCommand(state, command)).Status);
        Assert.False(app.View.ReadGameState().RetryRequested);
        Assert.False(app.View.ReadGameState().WaitingForInput);
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
            state => new(true, state with { SessionId = state.SessionId + 1 })));
        Assert.Same(before, app.View.ReadGameState());
    }
}
