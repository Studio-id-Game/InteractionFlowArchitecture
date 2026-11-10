using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Externals;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class ResultInteractionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ReturnedFailurePreservesEndAndDoesNotReadInput(bool failEnable, bool cancellationAsResult)
    {
        using var app = new EscapeApplicationContext();
        using var context = CreateResultContext(app);
        Exception failure = cancellationAsResult
            ? new OperationCanceledException("returned cancellation")
            : new InvalidOperationException("returned failure");
        var failed = ResultReaction.End(failure);
        var reaction = new ResultReaction
        {
            Show = _ => new(failEnable ? ResultReaction.End() : failed),
            Enable = _ => new(failed)
        };
        var input = new UnexpectedInputOperation();
        var interaction = new HandleResult(input, reaction, new GameExceptionHandling(), new GameCancellationHandling());

        var end = await interaction.ExecuteAsync(context);

        Assert.Same(failed, end.End);
        Assert.Equal(failEnable ? 1 : 0, reaction.Enabled);
        Assert.Equal(0, input.Reads);
        Assert.False(context.Lifetime.ShutdownRequested);
        Assert.False(context.Cancellation.HasTask);
    }

    [Fact]
    public async Task ReturnedCommandFailurePreservesEndWithoutRetrying()
    {
        using var app = new EscapeApplicationContext();
        using var context = CreateResultContext(app);
        var failed = ResultReaction.End(new InvalidOperationException("command failed"));
        var reaction = new ResultReaction { CommandFailure = failed };
        var execution = Create(reaction).ExecuteAsync(context);
        await WaitForInputAsync(app);
        Assert.True(app.View.TrySubmitCommand(context.SessionId, GameCommand.Retry));

        var end = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Same(failed, end.End);
        Assert.False(context.Gameplay.ReadGameState(context.SessionId).State.RetryRequested);
        Assert.False(app.View.ReadGameState().Info.WaitingForInput);
        Assert.False(context.Lifetime.ShutdownRequested);
        Assert.False(context.Cancellation.HasTask);
    }

    [Fact]
    public async Task ResultPresentationMustCompleteBeforeInputIsEnabledAndRetryIsReceived()
    {
        using var app = new EscapeApplicationContext();
        using var context = CreateResultContext(app);
        var pending = new TaskCompletionSource<ReactionEnd>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reaction = new ResultReaction { Show = _ => new(pending.Task) };
        var execution = Create(reaction).ExecuteAsync(context);
        Assert.False(execution.IsCompleted);
        Assert.Equal(0, reaction.Enabled);

        pending.SetResult(ResultReaction.End());
        await WaitForInputAsync(app);
        Assert.Equal(1, reaction.Enabled);
        Assert.False(execution.IsCompleted);
        Assert.True(app.View.TrySubmitCommand(context.SessionId, GameCommand.Up));
        await WaitForInputAsync(app);
        Assert.False(execution.IsCompleted);
        Assert.Equal(InputFeedback.StageDoesNotAcceptSelection, app.View.ReadGameState().Info.Feedback);
        Assert.True(app.View.TrySubmitCommand(context.SessionId, GameCommand.Retry));
        Assert.False((await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).HasException);
        Assert.True(app.View.ReadGameState().State.RetryRequested);
        Assert.False(context.Cancellation.HasTask);
    }

    [Fact]
    public async Task PresentationFailureDoesNotOpenInput()
    {
        using var app = new EscapeApplicationContext();
        using var context = CreateResultContext(app);
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
        using var context = CreateResultContext(app);
        var failure = new InvalidOperationException("enable failed");
        var reaction = new ResultReaction { Enable = _ => throw failure };
        var end = await Create(reaction).ExecuteAsync(context);
        Assert.Same(failure, end.Exception);
        Assert.True(context.Lifetime.ShutdownRequested);
        Assert.False(context.Cancellation.HasTask);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task StopOrCancellationAfterResultStageDoesNotLeaveAWait(bool stopAfterEnable, bool cancel)
    {
        using var app = new EscapeApplicationContext();
        using var context = CreateResultContext(app);
        ReactionEnd Stop(IGameplayContext current)
        {
            current.Lifetime.RequestShutdown();
            if (cancel) current.Cancellation.Cancel();
            return ResultReaction.End();
        }
        var reaction = new ResultReaction
        {
            Show = current => new(stopAfterEnable ? ResultReaction.End() : Stop(current)),
            Enable = current => new(Stop(current))
        };
        var input = new UnexpectedInputOperation();
        var interaction = new HandleResult(input, reaction, new GameExceptionHandling(), new GameCancellationHandling());
        var end = await interaction.ExecuteAsync(context)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(end.HasException);
        Assert.Equal(stopAfterEnable ? 1 : 0, reaction.Enabled);
        Assert.Equal(0, input.Reads);
        Assert.False(context.Cancellation.HasTask);
        Assert.False(app.View.ReadGameState().Info.WaitingForInput);
    }

    [Fact]
    public async Task CancellationWhileWaitingForRetryStopsInputAndReleasesTheInteraction()
    {
        using var app = new EscapeApplicationContext();
        using var context = CreateResultContext(app);
        var execution = Create(new ResultReaction()).ExecuteAsync(context);
        await WaitForInputAsync(app);

        context.Cancellation.Cancel();

        var end = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(end.HasException);
        Assert.True(context.Lifetime.ShutdownRequested);
        Assert.False(app.View.ReadGameState().Info.WaitingForInput);
        Assert.False(context.Cancellation.HasTask);
        Assert.False(app.View.ReadGameState().State.RetryRequested);
    }

    private static HandleResult Create(IGameplayReaction reaction)
        => new(new GameInputOperation(), reaction, new GameExceptionHandling(), new GameCancellationHandling());

    private static GameplayContext CreateResultContext(EscapeApplicationContext app)
    {
        var context = GameTestSteps.Ready(app);
        GameTestSteps.PlayTurn(context, app, GameCommand.Right);
        GameTestSteps.PlayTurn(context, app, GameCommand.Up);
        var result = app.Gameplay.UpdateGameState(context.SessionId,
            (state, _, _, completed) => WorldRules.BeginResultPresentation(state, completed));
        GameTestSteps.Complete(app, result.Presentation!.Value);
        return context;
    }

    private static async Task WaitForInputAsync(IViewContext context)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!context.View.ReadGameState().Info.WaitingForInput)
        {
            TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
            Assert.True(DateTime.UtcNow < deadline, "Result input did not start.");
            await Task.Yield();
        }
    }

    private sealed class UnexpectedInputOperation : Operation, IGameInputOperation
    {
        public int Reads { get; private set; }
        public override void ForceResetMemoryState() { }
        public ValueTask<GameCommand> ReadCommandAsync(IGameplayContext context)
        {
            Reads++;
            throw new InvalidOperationException("Input must not be read after a failed reaction.");
        }
    }

    private sealed class ResultReaction : Reaction, IGameplayReaction
    {
        public ReactionEnd ApplyControlRequest(IGameplayContext context) => GetEnd();
        public Func<IGameplayContext, ValueTask<ReactionEnd>> Show { get; init; } = _ => new(End());
        private readonly GameplayReaction actual = new();
        public Func<IGameplayContext, ValueTask<ReactionEnd>>? Enable { get; init; }
        public ReactionEnd? CommandFailure { get; init; }
        public int Enabled { get; private set; }
        public static ReactionEnd End(Exception? failure = null) => GetEnd(failure);
        public override void ForceResetMemoryState() { }
        public ValueTask<ReactionEnd> PresentResultAsync(IGameplayContext context) => Show(context);
        public ValueTask<ReactionEnd> EnableResultInputAsync(IGameplayContext context)
        {
            Enabled++;
            return Enable is { } enable ? enable(context) : actual.EnableResultInputAsync(context);
        }
        public ValueTask<ReactionEnd> BeginSessionAsync(IGameplayContext context) => throw new NotSupportedException();
        public ValueTask<ReactionEnd> BeginActionSelectionAsync(IGameplayContext context) => throw new NotSupportedException();
        public ValueTask<ReactionEnd> HandleCommandAsync(IGameplayContext context, GameCommand command)
            => CommandFailure is { } failure ? new(failure) : actual.HandleCommandAsync(context, command);
        public ValueTask<ReactionEnd> ApplyPlayerActionAsync(IGameplayContext context) => throw new NotSupportedException();
        public ValueTask<ReactionEnd> ApplyEnemyActionAsync(IGameplayContext context) => throw new NotSupportedException();
    }
}
