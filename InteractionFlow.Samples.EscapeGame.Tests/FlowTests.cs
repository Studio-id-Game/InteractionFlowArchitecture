using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Builders;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Externals;
using InteractionFlow.Samples.EscapeGame.Interactions;
using InteractionFlow.Samples.EscapeGame.SystemFlows;
using InteractionFlow.Standard.Builders;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class FlowTests
{
    [Fact]
    public async Task SharedCancellationAfterSuccessfulReactionIsNormalAtTheNextCheckpoint()
    {
        using var fixture = new Fixture();
        var canceledAtBoundary = false;
        fixture.Game.AfterBegin = context =>
        {
            Assert.False(context.Lifetime.ShutdownRequested);
            context.Cancellation.Cancel();
            canceledAtBoundary = true;
        };

        var end = await fixture.Flow.ExecuteAsync(fixture.Context);

        Assert.True(canceledAtBoundary);
        Assert.False(end.HasException);
        Assert.True(fixture.Session.Ended);
        Assert.NotNull(fixture.Game.Cancellation);
        Assert.False(fixture.Game.Cancellation.HasTask);
        Assert.Equal("close", fixture.Window.Events[^1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnrequestedOrUnrelatedReactionCancellationRemainsFailure(bool unrelatedToken)
    {
        using var fixture = new Fixture();
        using var unrelated = new CancellationTokenSource();
        unrelated.Cancel();
        var failure = unrelatedToken
            ? new OperationCanceledException("unrelated cancellation", unrelated.Token)
            : new OperationCanceledException("unrequested cancellation");
        fixture.Game.AfterBegin = context =>
        {
            Assert.False(context.Cancellation.IsCancellationRequested);
            throw failure;
        };

        var end = await fixture.Flow.ExecuteAsync(fixture.Context);

        Assert.True(end.HasException);
        Assert.True(Contains(end.Exception, failure));
        Assert.True(fixture.Session.Ended);
        Assert.Equal("close", fixture.Window.Events[^1]);
        Assert.NotNull(fixture.Game.Cancellation);
        Assert.False(fixture.Game.Cancellation.HasTask);
    }

    [Fact]
    public async Task PreCanceledGameStillOpensAndClosesTheIndependentWindowFrames()
    {
        using var fixture = new Fixture();
        fixture.Context.Cancellation.Cancel();

        var end = await fixture.Flow.ExecuteAsync(fixture.Context);

        Assert.False(end.HasException);
        Assert.Equal("open", fixture.Window.Events[0]);
        Assert.Equal("close", fixture.Window.Events[^1]);
        Assert.True(fixture.Context.Lifetime.ShutdownRequested);
        Assert.True(fixture.Session.Ended);
        Assert.False(fixture.Context.Cancellation.HasTask);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExternalWindowCancellationStopsPresentationWaitAndJoinsEvenIfStopFails(bool failStop)
    {
        var stopFailure = new InvalidOperationException("stop failed during external cancellation");
        using var fixture = new Fixture(failStop ? stopFailure : null);
        fixture.Window.CompletePresentations = false;
        var canceledDuringPresentation = false;
        var gameObservedBeforeTheNextFrame = false;
        var requestedToken = fixture.Context.Cancellation.GetToken();
        var closeSawJoinedGame = false;
        fixture.Window.BeforeClose = () => closeSawJoinedGame = fixture.Session.Ended
            && fixture.Game.Cancellation is { HasTask: false };
        fixture.Input.PollFrame = frames =>
        {
            if (canceledDuringPresentation || frames.View.CaptureSnapshot().Presentation is null)
                return;
            Assert.NotSame(fixture.Context.Cancellation, frames.Cancellation);
            fixture.Context.Cancellation.Cancel();
            canceledDuringPresentation = true;
            // メイン側がこのフレーム内に留まっていても、ゲーム待機へ通知が届きます。
            gameObservedBeforeTheNextFrame = SpinWait.SpinUntil(
                () => fixture.Game.CancellationObserved,
                TimeSpan.FromSeconds(5));
        };

        var end = await fixture.Flow.ExecuteAsync(fixture.Context);

        Assert.True(canceledDuringPresentation);
        Assert.True(gameObservedBeforeTheNextFrame);
        Assert.True(closeSawJoinedGame);
        Assert.NotNull(fixture.Game.Cancellation);
        Assert.False(fixture.Game.Cancellation.HasTask);
        Assert.Same(fixture.Context.Cancellation, fixture.Game.Cancellation);
        Assert.Equal(failStop, end.HasException);
        if (failStop)
            Assert.True(Contains(end.Exception, stopFailure));
        Assert.Equal("close", fixture.Window.Events[^1]);
        Assert.True(fixture.Context.Lifetime.ShutdownRequested);
        Assert.True(requestedToken.IsCancellationRequested);
        Assert.False(fixture.Context.Cancellation.HasTask);
    }

    [Fact]
    public async Task InitialPresentationWaitKeepsFramesAliveUntilRenderedAcknowledgment()
    {
        using var fixture = new Fixture();
        var heldFrames = 0;
        fixture.Window.CompletePresentations = false;
        fixture.Input.PollFrame = context =>
        {
            var snapshot = context.View.CaptureSnapshot();
            if (snapshot.SessionId == EscapeApplicationContext.NoSessionId)
                return;

            heldFrames++;
            Assert.Equal(GameStage.Preparing, snapshot.Stage);
            Assert.False(snapshot.AcceptingInput);
            Assert.False(context.View.TrySubmitCommand(snapshot.SessionId, GameCommand.Up));
            if (heldFrames >= 5)
                context.GetRequired<FrameState>().ShutdownRequested = true;
        };

        var end = await fixture.Flow.ExecuteAsync(fixture.Context);

        Assert.False(end.HasException);
        Assert.Equal(5, heldFrames);
        Assert.True(fixture.Window.DrawCount >= 5);
        Assert.Equal("close", fixture.Window.Events[^1]);
        Assert.False(fixture.Context.Cancellation.HasTask);
    }

    [Fact]
    public async Task TwelveTurnSolutionPresentsResultThenRetriesWithNewSession()
    {
        using var fixture = new Fixture();
        var commands = new Queue<GameCommand>(Solution.SelectMany(command => new[] { command, GameCommand.Confirm }));
        GameViewSnapshot? won = null;
        long firstSession = 0;
        long secondSession = 0;
        fixture.Input.PollFrame = context =>
        {
            var snapshot = context.View.CaptureSnapshot();
            if (snapshot.SessionId == EscapeApplicationContext.NoSessionId)
                return;
            if (firstSession == 0)
                firstSession = snapshot.SessionId;

            var input = context.GetRequired<FrameState>();
            if (snapshot.SessionId != firstSession)
            {
                secondSession = snapshot.SessionId;
                Assert.Equal(0, snapshot.Turn);
                Assert.Equal(snapshot.Room.PlayerStart, snapshot.Player);
                Assert.False(snapshot.HasKey);
                Assert.False(snapshot.DoorUnlocked);
                input.ShutdownRequested = true;
                return;
            }

            if (!snapshot.AcceptingInput)
                return;
            if (commands.TryDequeue(out var command))
            {
                input.Command = command;
                return;
            }

            won = snapshot;
            Assert.Equal(GameStage.Result, snapshot.Stage);
            Assert.Equal(GameOutcome.Won, snapshot.Outcome);
            input.Command = GameCommand.Retry;
        };

        var end = await fixture.Flow.ExecuteAsync(fixture.Context);

        Assert.False(end.HasException);
        Assert.NotNull(won);
        Assert.Equal(12, won.Turn);
        Assert.True(won.KeyCollected);
        Assert.False(won.HasKey);
        Assert.True(won.SwitchActivated);
        Assert.True(won.DoorUnlocked);
        Assert.Equal(3, won.EnemyPatrolIndex);
        Assert.True(secondSession > firstSession);
        Assert.All(fixture.Window.Threads, thread => Assert.Equal(fixture.Window.Threads[0], thread));
    }

    [Fact]
    public async Task PausedPresentationAndInputRemainFrozenWhileFramesContinueThenResume()
    {
        using var fixture = new Fixture();
        var phase = 0;
        var pausedFrames = 0;
        GameViewSnapshot? frozen = null;
        fixture.Input.PollFrame = context =>
        {
            var snapshot = context.View.CaptureSnapshot();
            var input = context.GetRequired<FrameState>();
            if (phase == 0 && snapshot.AcceptingInput)
            {
                input.Command = GameCommand.Up;
                phase = 1;
            }
            else if (phase == 1 && snapshot.AcceptingInput)
            {
                input.Command = GameCommand.Confirm;
                phase = 2;
                fixture.Window.CompletePresentations = false;
            }
            else if (phase == 2 && snapshot.Stage == GameStage.PlayerPresenting)
            {
                input.PauseRequested = true;
                phase = 3;
            }
            else if (phase == 3 && snapshot.Mode == ExecutionMode.Paused)
            {
                frozen ??= snapshot;
                Assert.Equal(frozen.Player, snapshot.Player);
                Assert.Equal(frozen.Enemy, snapshot.Enemy);
                Assert.Equal(frozen.Turn, snapshot.Turn);
                Assert.Equal(frozen.Stage, snapshot.Stage);
                Assert.Equal(frozen.Presentation!.Key, snapshot.Presentation!.Key);
                Assert.Equal(frozen.Presentation.ElapsedSeconds, snapshot.Presentation.ElapsedSeconds);
                Assert.False(context.View.TrySubmitCommand(snapshot.SessionId, GameCommand.Confirm));
                if (++pausedFrames == 5)
                {
                    input.PauseRequested = true;
                    phase = 4;
                    fixture.Window.CompletePresentations = true;
                }
            }
            else if (phase == 4 && snapshot.AcceptingInput)
            {
                Assert.Equal(1, snapshot.Turn);
                input.ShutdownRequested = true;
            }
        };

        var end = await fixture.Flow.ExecuteAsync(fixture.Context);

        Assert.False(end.HasException);
        Assert.Equal(5, pausedFrames);
        Assert.Equal(4, phase);
    }

    [Fact]
    public async Task ShutdownBeforeWorkerStartsIsSuccessful()
    {
        using var fixture = new Fixture();
        fixture.Window.OnOpen = context => context.Lifetime.RequestShutdown();

        var end = await fixture.Flow.ExecuteAsync(fixture.Context);

        Assert.False(end.HasException);
        Assert.Equal("close", fixture.Window.Events[^1]);
        Assert.False(fixture.Context.Cancellation.HasTask);
    }

    [Fact]
    public async Task StopPortFailureStillCancelsJoinsAndClosesWithoutNextPresentation()
    {
        var failure = new InvalidOperationException("stop failed");
        using var fixture = new Fixture(failure);
        fixture.Window.CompletePresentations = false;
        fixture.Input.PollFrame = context =>
        {
            if (context.View.CaptureSnapshot().Presentation is not null)
                context.GetRequired<FrameState>().ShutdownRequested = true;
        };

        var end = await fixture.Flow.ExecuteAsync(fixture.Context);

        Assert.True(end.HasException);
        Assert.True(Contains(end.Exception, failure));
        Assert.Equal("close", fixture.Window.Events[^1]);
        Assert.False(fixture.Context.Cancellation.HasTask);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClosingCancellationPreservesPriorFailureAndCancellation(bool earlierFailure)
    {
        var previous = new FormatException("earlier failed");
        var canceled = new OperationCanceledException("close canceled");
        using var context = new EscapeApplicationContext();
        context.With(new CleanupInfo(CleanupKind.CloseWindow, earlierFailure ? previous : Result.Success));
        var window = new HeadlessWindowReaction { CloseFailure = canceled };
        var interaction = new CloseWindow(window, new GameExceptionHandling(), new GameCancellationHandling());

        var end = await interaction.ExecuteAsync(context);

        Assert.True(end.HasException);
        Assert.True(Contains(end.Exception, canceled));
        if (earlierFailure)
            Assert.True(Contains(end.Exception, previous));
    }

    [Fact]
    public async Task EndSessionCancellationPreservesEarlierFailureAndResetFailure()
    {
        var previous = new FormatException("session failed");
        var canceled = new OperationCanceledException("end session canceled");
        using var context = new EscapeApplicationContext();
        context.With(new CleanupInfo(CleanupKind.EndSession, previous));
        var port = new CanceledGameLifetimeReaction(canceled);
        var interaction = new EndSession(port, new GameExceptionHandling(), new GameCancellationHandling());

        var end = await interaction.ExecuteAsync(context);

        Assert.True(end.HasException);
        Assert.True(Contains(end.Exception, previous));
        Assert.True(Contains(end.Exception, canceled));
        Assert.True(ContainsMessage(end.Exception, "Cancellation is not requested."));
        Assert.True(context.Lifetime.ShutdownRequested);
        Assert.False(context.Cancellation.HasTask);
    }

    private static readonly GameCommand[] Solution =
    [
        GameCommand.Up, GameCommand.Up, GameCommand.Down, GameCommand.Down,
        GameCommand.Right, GameCommand.Interact, GameCommand.Right, GameCommand.Right,
        GameCommand.Right, GameCommand.Interact, GameCommand.Up, GameCommand.Up
    ];

    private static bool Contains(Exception? actual, Exception expected)
    {
        if (ReferenceEquals(actual, expected))
            return true;
        if (actual is AggregateException aggregate && aggregate.InnerExceptions.Any(inner => Contains(inner, expected)))
            return true;
        return actual?.InnerException is not null && Contains(actual.InnerException, expected);
    }

    private static bool ContainsMessage(Exception? actual, string message)
    {
        if (actual?.Message == message)
            return true;
        if (actual is AggregateException aggregate && aggregate.InnerExceptions.Any(inner => ContainsMessage(inner, message)))
            return true;
        return actual?.InnerException is not null && ContainsMessage(actual.InnerException, message);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly InteractionFlow.Core.Builders.ScopeHandler scope;
        public EscapeApplicationContext Context { get; } = new();
        public InteractionFlow.Core.Builders.SystemFlowHandler<IApplicationContext> Flow { get; }
        public ScriptedWindowOperation Input { get; }
        public HeadlessWindowReaction Window { get; }
        public TrackingGameplayReaction Game { get; }
        public TrackingGameLifetimeReaction Session { get; }

        public Fixture(Exception? stopFailure = null)
        {
            Context.With("application-only value");
            var builder = new ScopeBuilder();
            builder.Apply(new EscapeGameProfile())
                .UseFunction<IWindowOperation, ScriptedWindowOperation>()
                .UseFunction<IWindowReaction, HeadlessWindowReaction>()
                .UseFunction<IGameplayReaction, TrackingGameplayReaction>()
                .UseFunction<IGameLifetimeReaction, TrackingGameLifetimeReaction>();
            scope = builder.BuildScope();
            Input = Assert.IsType<ScriptedWindowOperation>(scope.GetRequiredService<IWindowOperation>());
            Window = Assert.IsType<HeadlessWindowReaction>(scope.GetRequiredService<IWindowReaction>());
            Game = Assert.IsType<TrackingGameplayReaction>(scope.GetRequiredService<IGameplayReaction>());
            Session = Assert.IsType<TrackingGameLifetimeReaction>(scope.GetRequiredService<IGameLifetimeReaction>());
            Session.Failure = stopFailure;
            Flow = scope.BuildSystemFlow<WindowSystemFlow, IApplicationContext>();
        }

        public void Dispose()
        {
            Flow.Dispose();
            scope.Dispose();
            Context.Dispose();
        }
    }

    public sealed class ScriptedWindowOperation : Operation, IWindowOperation
    {
        private readonly Stopwatch timeout = Stopwatch.StartNew();
        public Action<IGameViewContext>? PollFrame { get; set; }
        public override void ForceResetMemoryState() { }
        public void PollFrameInput(IGameViewContext context)
        {
            Assert.False(context.TryGet<IGameplayAccess>(out _));
            Assert.False(context.TryGet<string>(out _));
            if (timeout.Elapsed > TimeSpan.FromSeconds(10))
                throw new TimeoutException("The scripted game did not finish.");
            var frame = context.GetRequired<FrameState>();
            var snapshot = context.View.CaptureSnapshot();
            frame.SessionId = snapshot.SessionId;
            frame.Command = null;
            frame.PauseRequested = false;
            frame.SkipRequested = false;
            frame.ShutdownRequested = false;
            frame.Presentation = snapshot.Presentation?.Key;
            PollFrame?.Invoke(context);
        }
    }

    public sealed class HeadlessWindowReaction : Reaction, IWindowReaction
    {
        public List<string> Events { get; } = [];
        public List<int> Threads { get; } = [];
        public int DrawCount { get; private set; }
        public bool CompletePresentations { get; set; } = true;
        public Exception? CloseFailure { get; set; }
        public Action<IGameViewContext>? OnOpen { get; set; }
        public Action? BeforeClose { get; set; }
        public override void ForceResetMemoryState() { }
        public ReactionEnd Open(IGameViewContext context)
        {
            Record("open");
            OnOpen?.Invoke(context);
            return GetEnd();
        }
        public ReactionEnd DrawFrame(IGameViewContext context)
        {
            Record("draw");
            DrawCount++;
            var snapshot = context.View.CaptureSnapshot();
            if (snapshot.Presentation is { } presentation)
            {
                context.View.AdvancePresentation(presentation.Key, 0, snapshot.ControlRevision);
                if (CompletePresentations)
                {
                    context.View.AdvancePresentation(presentation.Key, presentation.DurationSeconds, snapshot.ControlRevision);
                    context.View.TryCompletePresentation(presentation.Key);
                }
            }
            Thread.Yield();
            return GetEnd();
        }
        public ReactionEnd Close(ILifetimeContext context, Result previous)
        {
            Record("close");
            BeforeClose?.Invoke();
            if (CloseFailure is not null)
                throw CloseFailure;
            return GetEnd(previous);
        }
        private void Record(string name)
        {
            Events.Add(name);
            Threads.Add(Environment.CurrentManagedThreadId);
        }
    }

    public sealed class TrackingGameLifetimeReaction : Reaction, IGameLifetimeReaction
    {
        public Exception? Failure { get; set; }
        public bool Ended { get; private set; }
        public override void ForceResetMemoryState() { }
        public ReactionEnd RequestShutdown(ILifetimeContext context)
        {
            if (Failure is not null)
                throw Failure;
            context.Lifetime.RequestShutdown();
            return GetEnd();
        }
        public ReactionEnd EndSession(IGameplayContext context, Result previous)
        {
            context.Gameplay.EndSession(context.SessionId);
            Ended = true;
            return GetEnd(previous);
        }
    }

    public sealed class TrackingGameplayReaction : Reaction, IGameplayReaction
    {
        private readonly GameplayReaction inner = new();
        private volatile bool cancellationObserved;
        public CancellationObject? Cancellation { get; private set; }
        public bool CancellationObserved => cancellationObserved;
        public Action<IGameplayContext>? AfterBegin { get; set; }
        public override void ForceResetMemoryState() { }
        public async ValueTask<ReactionEnd> BeginSessionAsync(IGameplayContext context)
        {
            Assert.False(context.TryGet<IGameViewAccess>(out _));
            Assert.False(context.TryGet<string>(out _));
            Cancellation = context.Cancellation;
            try
            {
                var end = await inner.BeginSessionAsync(context);
                AfterBegin?.Invoke(context);
                return end;
            }
            catch (OperationCanceledException)
            {
                cancellationObserved = true;
                throw;
            }
        }
        public ValueTask<ReactionEnd> BeginActionSelectionAsync(IGameplayContext context)
            => inner.BeginActionSelectionAsync(context);
        public ValueTask<ReactionEnd> HandleCommandAsync(IGameplayContext context, GameCommand command)
            => inner.HandleCommandAsync(context, command);
        public ValueTask<ReactionEnd> ApplyPlayerActionAsync(IGameplayContext context)
            => inner.ApplyPlayerActionAsync(context);
        public ValueTask<ReactionEnd> ApplyEnemyActionAsync(IGameplayContext context)
            => inner.ApplyEnemyActionAsync(context);
        public ValueTask<ReactionEnd> PresentResultAsync(IGameplayContext context)
            => inner.PresentResultAsync(context);
        public ValueTask<ReactionEnd> EnableResultInputAsync(IGameplayContext context)
            => inner.EnableResultInputAsync(context);
    }

    private sealed class CanceledGameLifetimeReaction(OperationCanceledException canceled) : Reaction, IGameLifetimeReaction
    {
        public override void ForceResetMemoryState() { }
        public ReactionEnd RequestShutdown(ILifetimeContext context) => GetEnd();
        public ReactionEnd EndSession(IGameplayContext context, Result previous) => throw canceled;
    }
}
