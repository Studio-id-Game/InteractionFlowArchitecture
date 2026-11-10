using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Externals;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class LifetimeAccessTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommonHandlersOnlyRequireLifetimeAccess(bool cancellation)
    {
        var lifetime = new Lifetime();
        using var context = new FlowContext().With<ILifetimeAccess>(lifetime);
        Assert.False(context.TryGet<IGameplayAccess>(out _));
        Assert.False(context.TryGet<IViewAccess>(out _));
        var failure = new InvalidOperationException("original failure");
        var interaction = new FailingInteraction(failure);
        if (cancellation)
            context.Cancellation.Cancel();

        var end = await interaction.ExecuteAsync(context);

        Assert.True(lifetime.ShutdownRequested);
        Assert.Equal(1, lifetime.StopCalls);
        Assert.False(context.Cancellation.HasTask);
        if (cancellation)
        {
            Assert.False(end.HasException);
            Assert.False(context.Cancellation.IsCancellationRequested);
        }
        else
            Assert.Same(failure, end.Exception);
    }

    [Fact]
    public void GameplayAllocatesUnpublishedSessionsAndRejectsCreationAfterShutdown()
    {
        using var app = new EscapeApplicationContext();
        var first = app.Gameplay.AllocateSession();
        var second = app.Gameplay.AllocateSession();
        Assert.True(first > EscapeApplicationContext.NoSessionId);
        Assert.True(second > first);
        Assert.Equal(EscapeApplicationContext.NoSessionId, app.View.ReadGameState().State.SessionId);
        Assert.Equal(GameUpdateStatus.Applied, app.Gameplay.BeginSession(first, WorldRules.BeginSession).Status);
        Assert.Equal(first, app.View.ReadGameState().State.SessionId);
        Assert.Equal(GameUpdateStatus.Rejected, app.Gameplay.BeginSession(first,
            _ => throw new InvalidOperationException("A published session must not be initialized again.")).Status);
        Assert.Equal(GameUpdateStatus.Rejected, app.Gameplay.BeginSession(second, WorldRules.BeginSession).Status);
        app.Gameplay.EndSession(first);
        Assert.Equal(GameUpdateStatus.Applied, app.Gameplay.BeginSession(second, WorldRules.BeginSession).Status);
        app.Lifetime.RequestShutdown();
        Assert.Throws<InvalidOperationException>(() => app.Gameplay.AllocateSession());
        Assert.Equal(ExecutionMode.Stopping, app.Gameplay.ReadGameState(second).Info);
    }

    [Fact]
    public async Task StoppedSessionCannotResumeOrBeRepublishedAndDoesNotBlockTheNextSession()
    {
        using var app = new EscapeApplicationContext();
        using var session = GameTestSteps.Ready(app);
        var id = session.SessionId;
        var input = app.Gameplay.ReadCommandAsync(id, TestContext.Current.CancellationToken).AsTask();
        Assert.True(app.Gameplay.SetPaused(id, true));
        var resume = app.Gameplay.WaitForActiveAsync(id, TestContext.Current.CancellationToken).AsTask();

        var stopped = app.Gameplay.UpdateGameState(id,
            (state, _, _, _) => new(state, new(true) { Mode = ExecutionMode.Stopping }), allowWhilePaused: true);

        Assert.Equal(GameUpdateStatus.Applied, stopped.Status);
        Assert.Equal(GameCommand.Stop, await input);
        Assert.False(await resume);
        Assert.False(app.Lifetime.ShutdownRequested);
        Assert.Equal(GameUpdateStatus.Stopped, app.Gameplay.BeginSession(id,
            _ => throw new InvalidOperationException("A stopped session must not be initialized again.")).Status);
        Assert.Equal(GameUpdateStatus.Stopped, app.Gameplay.UpdateGameState(id,
            (_, _, _, _) => throw new InvalidOperationException("A stopped session must not receive updates."), allowWhilePaused: true).Status);
        Assert.False(app.View.TrySubmitCommand(id, GameCommand.Up));
        Assert.False(app.View.TrySubmitControl(new(id, GameControlCommand.TogglePause)));

        // 停止したセッションが辞書に残っていても次を公開でき、旧セッションの終了と干渉しません。
        using var next = GameTestSteps.Ready(app);
        var current = app.View.ReadGameState();
        app.Gameplay.EndSession(id);
        Assert.Same(current, app.View.ReadGameState());
        Assert.Equal(next.SessionId, current.State.SessionId);
        Assert.Equal(ExecutionMode.Active, current.Info.Mode);
    }

    private sealed class Lifetime : ILifetimeAccess
    {
        public bool ShutdownRequested { get; private set; }
        public int StopCalls { get; private set; }
        public void RequestShutdown()
        {
            StopCalls++;
            ShutdownRequested = true;
        }
    }

    private sealed class FailingInteraction(Exception failure)
        : Interaction(new GameExceptionHandling(), new GameCancellationHandling())
    {
        protected override ValueTask<ReactionEnd> ExecuteCoreAsync(IFlowContext context) => throw failure;
    }
}
