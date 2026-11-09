using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Externals;
using InteractionFlow.Samples.EscapeGame.Interactions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class ResultInteractionTests
{
    [Fact]
    public async Task ResultPresentationMustCompleteBeforeInputIsEnabled()
    {
        using var app = new EscapeApplicationContext();
        using var context = new GameplayContext(app, ContextInheritance.Cancellation);
        var pending = new TaskCompletionSource<ReactionEnd>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reaction = new ResultReaction { Show = _ => new(pending.Task) };
        var execution = Create(reaction).ExecuteAsync(context);
        Assert.False(execution.IsCompleted);
        Assert.Equal(0, reaction.Enabled);

        pending.SetResult(ResultReaction.End());
        Assert.False((await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).HasException);
        Assert.Equal(1, reaction.Enabled);
        Assert.False(context.Cancellation.HasTask);
    }

    [Fact]
    public async Task PresentationFailureDoesNotOpenInput()
    {
        using var app = new EscapeApplicationContext();
        using var context = new GameplayContext(app, ContextInheritance.Cancellation);
        var failure = new InvalidOperationException("presentation failed");
        var reaction = new ResultReaction { Show = _ => new(ResultReaction.End(failure)) };
        var end = await Create(reaction).ExecuteAsync(context);
        Assert.Same(failure, end.Exception);
        Assert.Equal(0, reaction.Enabled);
    }

    [Fact]
    public async Task InputEnableFailureUsesExistingExceptionHandling()
    {
        using var app = new EscapeApplicationContext();
        using var context = new GameplayContext(app, ContextInheritance.Cancellation);
        var failure = new InvalidOperationException("enable failed");
        var reaction = new ResultReaction { Enable = _ => throw failure };
        var end = await Create(reaction).ExecuteAsync(context);
        Assert.Same(failure, end.Exception);
        Assert.True(context.Lifetime.ShutdownRequested);
        Assert.False(context.Cancellation.HasTask);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StopOrCancellationAfterPresentationDoesNotLeaveAWait(bool cancel)
    {
        using var app = new EscapeApplicationContext();
        using var context = GameTestSteps.CreateSessionContext(app);
        var actual = new GameplayReaction();
        var reaction = new ResultReaction
        {
            Show = current =>
            {
                current.Lifetime.RequestShutdown();
                if (cancel) current.Cancellation.Cancel();
                return new(ResultReaction.End());
            },
            Enable = actual.EnableResultInputAsync
        };
        var end = await Create(reaction).ExecuteAsync(context)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(end.HasException);
        Assert.False(context.Cancellation.HasTask);
        Assert.False(app.View.CaptureSnapshot().AcceptingInput);
    }

    private static PresentResult Create(IGameplayReaction reaction)
        => new(reaction, new GameExceptionHandling(), new GameCancellationHandling());

    private sealed class ResultReaction : Reaction, IGameplayReaction
    {
        public Func<IGameplayContext, ValueTask<ReactionEnd>> Show { get; init; } = _ => new(End());
        public Func<IGameplayContext, ValueTask<ReactionEnd>> Enable { get; init; } = _ => new(End());
        public int Enabled { get; private set; }
        public static ReactionEnd End(Exception? failure = null) => GetEnd(failure);
        public override void ForceResetMemoryState() { }
        public ValueTask<ReactionEnd> PresentResultAsync(IGameplayContext context) => Show(context);
        public ValueTask<ReactionEnd> EnableResultInputAsync(IGameplayContext context)
        {
            Enabled++;
            return Enable(context);
        }
        public ValueTask<ReactionEnd> BeginSessionAsync(IGameplayContext context) => throw new NotSupportedException();
        public ValueTask<ReactionEnd> BeginActionSelectionAsync(IGameplayContext context) => throw new NotSupportedException();
        public ValueTask<ReactionEnd> HandleCommandAsync(IGameplayContext context, GameCommand command) => throw new NotSupportedException();
        public ValueTask<ReactionEnd> ApplyPlayerActionAsync(IGameplayContext context) => throw new NotSupportedException();
        public ValueTask<ReactionEnd> ApplyEnemyActionAsync(IGameplayContext context) => throw new NotSupportedException();
    }
}
