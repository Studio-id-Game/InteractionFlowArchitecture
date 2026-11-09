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
            Assert.False(context.Session.ShutdownRequested);
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
        Assert.True(fixture.Context.Session.ShutdownRequested);
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
            if (canceledDuringPresentation || frames.Display.Snapshot().Presentation is null)
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
        Assert.True(fixture.Context.Session.ShutdownRequested);
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
            var snapshot = context.Display.Snapshot();
            if (snapshot.SessionId == EscapeApplicationContext.ApplicationSessionId)
                return;

            heldFrames++;
            Assert.Equal(GameStage.Preparing, snapshot.Stage);
            Assert.False(snapshot.AcceptingInput);
            Assert.False(context.Input.TrySubmit(snapshot.SessionId, GameCommand.Up));
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
        GameSnapshot? won = null;
        long firstSession = 0;
        long secondSession = 0;
        fixture.Input.PollFrame = context =>
        {
            var snapshot = context.Display.Snapshot();
            if (snapshot.SessionId == EscapeApplicationContext.ApplicationSessionId)
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
        GameSnapshot? frozen = null;
        fixture.Input.PollFrame = context =>
        {
            var snapshot = context.Display.Snapshot();
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
                Assert.False(context.Input.TrySubmit(snapshot.SessionId, GameCommand.Confirm));
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
        fixture.Window.OnOpen = context => context.Session.RequestShutdown();

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
            if (context.Display.Snapshot().Presentation is not null)
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
        using var context = new WindowContext();
        context.With(new Finishing(FinishingKind.CloseWindow, earlierFailure ? previous : Result.Success));
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
        context.With(new Finishing(FinishingKind.EndSession, previous));
        var port = new CanceledSessionReaction(canceled);
        var interaction = new EndSession(port, new GameExceptionHandling(), new GameCancellationHandling());

        var end = await interaction.ExecuteAsync(context);

        Assert.True(end.HasException);
        Assert.True(Contains(end.Exception, previous));
        Assert.True(Contains(end.Exception, canceled));
        Assert.True(ContainsMessage(end.Exception, "Cancellation is not requested."));
        Assert.True(context.Session.ShutdownRequested);
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
        public WindowContext Context { get; } = new();
        public InteractionFlow.Core.Builders.SystemFlowHandler<WindowContext> Flow { get; }
        public ScriptedWindowOperation Input { get; }
        public HeadlessWindowReaction Window { get; }
        public TrackingGameReaction Game { get; }
        public TrackingSessionReaction Session { get; }

        public Fixture(Exception? stopFailure = null)
        {
            var builder = new ScopeBuilder();
            builder.Apply(new EscapeGameProfile())
                .UseFunction<IWindowOperation, ScriptedWindowOperation>()
                .UseFunction<IWindowReaction, HeadlessWindowReaction>()
                .UseFunction<IGameReaction, TrackingGameReaction>()
                .UseFunction<ISessionReaction, TrackingSessionReaction>();
            scope = builder.BuildScope();
            Input = Assert.IsType<ScriptedWindowOperation>(scope.GetRequiredService<IWindowOperation>());
            Window = Assert.IsType<HeadlessWindowReaction>(scope.GetRequiredService<IWindowReaction>());
            Game = Assert.IsType<TrackingGameReaction>(scope.GetRequiredService<IGameReaction>());
            Session = Assert.IsType<TrackingSessionReaction>(scope.GetRequiredService<ISessionReaction>());
            Session.Failure = stopFailure;
            Flow = scope.BuildSystemFlow<WindowSystemFlow, WindowContext>();
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
        public Action<WindowContext>? PollFrame { get; set; }
        public override void ForceResetMemoryState() { }
        public void Poll(WindowContext context)
        {
            if (timeout.Elapsed > TimeSpan.FromSeconds(10))
                throw new TimeoutException("The scripted game did not finish.");
            var frame = context.GetRequired<FrameState>();
            var snapshot = context.Display.Snapshot();
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
        public Action<WindowContext>? OnOpen { get; set; }
        public Action? BeforeClose { get; set; }
        public override void ForceResetMemoryState() { }
        public ReactionEnd Open(WindowContext context)
        {
            Record("open");
            OnOpen?.Invoke(context);
            return GetEnd();
        }
        public ReactionEnd Draw(WindowContext context)
        {
            Record("draw");
            DrawCount++;
            var snapshot = context.Display.Snapshot();
            if (snapshot.Presentation is { } presentation)
            {
                context.Presentation.Advance(presentation.Key, 0, snapshot.ControlRevision);
                if (CompletePresentations)
                {
                    context.Presentation.Advance(presentation.Key, presentation.DurationSeconds, snapshot.ControlRevision);
                    context.Presentation.TryComplete(presentation.Key);
                }
            }
            Thread.Yield();
            return GetEnd();
        }
        public ReactionEnd Close(WindowContext context, Result previous)
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

    public sealed class TrackingSessionReaction : Reaction, ISessionReaction
    {
        public Exception? Failure { get; set; }
        public bool Ended { get; private set; }
        public override void ForceResetMemoryState() { }
        public ReactionEnd RequestShutdown(WindowContext context)
        {
            if (Failure is not null)
                throw Failure;
            context.Session.RequestShutdown();
            return GetEnd();
        }
        public ReactionEnd EndSession(EscapeApplicationContext context, Result previous)
        {
            context.Session.EndSession(context.SessionId);
            Ended = true;
            return GetEnd(previous);
        }
    }

    public sealed class TrackingGameReaction : Reaction, IGameReaction
    {
        private readonly GameReaction inner = new();
        private volatile bool cancellationObserved;
        public CancellationObject? Cancellation { get; private set; }
        public bool CancellationObserved => cancellationObserved;
        public Action<EscapeApplicationContext>? AfterBegin { get; set; }
        public override void ForceResetMemoryState() { }
        public async ValueTask<ReactionEnd> BeginAsync(EscapeApplicationContext context)
        {
            Cancellation = context.Cancellation;
            try
            {
                var end = await inner.BeginAsync(context);
                AfterBegin?.Invoke(context);
                return end;
            }
            catch (OperationCanceledException)
            {
                cancellationObserved = true;
                throw;
            }
        }
        public ValueTask<ReactionEnd> EnableSelectionAsync(EscapeApplicationContext context)
            => inner.EnableSelectionAsync(context);
        public ValueTask<ReactionEnd> SelectAsync(EscapeApplicationContext context, GameCommand command)
            => inner.SelectAsync(context, command);
        public ValueTask<ReactionEnd> ApplyPlayerAsync(EscapeApplicationContext context)
            => inner.ApplyPlayerAsync(context);
        public ValueTask<ReactionEnd> ApplyEnemyAsync(EscapeApplicationContext context)
            => inner.ApplyEnemyAsync(context);
        public ValueTask<ReactionEnd> ShowResultAsync(EscapeApplicationContext context)
            => inner.ShowResultAsync(context);
        public ValueTask<ReactionEnd> EnableResultInputAsync(EscapeApplicationContext context)
            => inner.EnableResultInputAsync(context);
    }

    private sealed class CanceledSessionReaction(OperationCanceledException canceled) : Reaction, ISessionReaction
    {
        public override void ForceResetMemoryState() { }
        public ReactionEnd RequestShutdown(WindowContext context) => GetEnd();
        public ReactionEnd EndSession(EscapeApplicationContext context, Result previous) => throw canceled;
    }
}
