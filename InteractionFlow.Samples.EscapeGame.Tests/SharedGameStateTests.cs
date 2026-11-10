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
                    state => new(GameUpdateStatus.Applied, state with { Turn = state.Turn + 1 })).Status);
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
            state => new(GameUpdateStatus.Applied, state with { SessionId = state.SessionId + 1 })));
        Assert.Same(before, app.View.ReadGameState());
    }
}
