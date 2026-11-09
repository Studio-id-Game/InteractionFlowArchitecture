using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Externals;
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
        using var context = new FlowContext().With<IApplicationLifetimeAccess>(lifetime);
        Assert.False(context.TryGet<IGameplayAccess>(out _));
        Assert.False(context.TryGet<IGameViewAccess>(out _));
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
        Assert.Equal(EscapeApplicationContext.NoSessionId, app.View.CaptureSnapshot().SessionId);
        Assert.Equal(GameUpdateStatus.Applied, app.Gameplay.BeginSession(first).Status);
        Assert.Equal(first, app.View.CaptureSnapshot().SessionId);
        Assert.Equal(GameUpdateStatus.Rejected, app.Gameplay.BeginSession(second).Status);
        app.Gameplay.EndSession(first);
        Assert.Equal(GameUpdateStatus.Applied, app.Gameplay.BeginSession(second).Status);
        app.Lifetime.RequestShutdown();
        Assert.Throws<InvalidOperationException>(() => app.Gameplay.AllocateSession());
        Assert.True(app.Gameplay.GetSessionProgress(second).Stopping);
    }

    private sealed class Lifetime : IApplicationLifetimeAccess
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
