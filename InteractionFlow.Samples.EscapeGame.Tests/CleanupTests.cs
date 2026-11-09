using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Core.Interactions;
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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class CleanupTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnrelatedResultDoesNotChangeNormalCancellationRecovery(bool failedResult)
    {
        using var context = new EscapeApplicationContext();
        var unrelated = new FormatException("unrelated result");
        context.With<Result>(failedResult ? unrelated : Result.Success);
        var interaction = new CancelNormally(new GameExceptionHandling(), new GameCancellationHandling());

        var end = await interaction.ExecuteAsync(context);

        Assert.False(end.HasException);
        Assert.False(context.TryGet<CleanupInfo>(out _));
        Assert.False(context.Cancellation.IsCancellationRequested);
        Assert.False(context.Cancellation.HasTask);
        Assert.Equal(!failedResult, context.GetRequired<Result>().Try(out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnrelatedResultDoesNotBecomePriorFailureForOrdinaryException(bool failedResult)
    {
        using var context = new EscapeApplicationContext();
        var unrelated = new FormatException("unrelated result");
        var failure = new InvalidOperationException("ordinary operation failed");
        context.With<Result>(failedResult ? unrelated : Result.Success);
        var interaction = new FailNormally(failure, new GameExceptionHandling(), new GameCancellationHandling());

        var end = await interaction.ExecuteAsync(context);

        Assert.Same(failure, end.Exception);
        Assert.False(Contains(end.Exception, unrelated));
        Assert.False(context.TryGet<CleanupInfo>(out _));
        Assert.False(context.Cancellation.HasTask);
        Assert.Equal(!failedResult, context.GetRequired<Result>().Try(out _));
    }

    [Theory]
    [InlineData(CleanupKind.CloseWindow, false, false)]
    [InlineData(CleanupKind.CloseWindow, false, true)]
    [InlineData(CleanupKind.CloseWindow, true, false)]
    [InlineData(CleanupKind.CloseWindow, true, true)]
    [InlineData(CleanupKind.EndSession, false, false)]
    [InlineData(CleanupKind.EndSession, false, true)]
    [InlineData(CleanupKind.EndSession, true, false)]
    [InlineData(CleanupKind.EndSession, true, true)]
    public async Task FinishingFailurePreservesOperationAndPriorFailures(
        CleanupKind kind,
        bool canceled,
        bool earlierFailure)
    {
        using var app = new EscapeApplicationContext();
        using var window = new GameViewContext(app, ContextInheritance.None);
        using var game = new GameplayContext(app, ContextInheritance.Cancellation);
        FlowContext context = kind == CleanupKind.CloseWindow ? window : game;
        var previous = new FormatException("earlier failure");
        Exception current = canceled
            ? new OperationCanceledException("finishing canceled")
            : new InvalidOperationException("finishing failed");
        context.With(new CleanupInfo(kind, earlierFailure ? previous : Result.Success));
        var ports = new FailingPorts(current);

        var end = await ExecuteFinishingAsync(kind, ports, window, game);

        Assert.True(end.HasException);
        Assert.True(Contains(end.Exception, current));
        if (earlierFailure)
            Assert.True(Contains(end.Exception, previous));
        if (canceled)
            Assert.True(ContainsMessage(end.Exception, "Cancellation is not requested."));
        Assert.Equal(1, ports.Calls);
        Assert.False(app.TryGet<CleanupInfo>(out _));
        Assert.False(context.Cancellation.HasTask);
    }

    [Theory]
    [InlineData(CleanupKind.CloseWindow)]
    [InlineData(CleanupKind.EndSession)]
    public async Task FinishingCancellationRetainsRegisteredTaskFailureDuringReset(CleanupKind kind)
    {
        using var app = new EscapeApplicationContext();
        using var window = new GameViewContext(app, ContextInheritance.None);
        using var game = new GameplayContext(app, ContextInheritance.Cancellation);
        FlowContext context = kind == CleanupKind.CloseWindow ? window : game;
        var previous = new FormatException("prior failure");
        var resetFailure = new InvalidOperationException("registered task failed");
        context.With(new CleanupInfo(kind, previous));
        using var registration = context.Cancellation.AddCancelableTask(Task.FromException(resetFailure));
        context.Cancellation.Cancel();
        var ports = new FailingPorts(new InvalidOperationException("must not execute"));

        var end = await ExecuteFinishingAsync(kind, ports, window, game);

        Assert.True(end.HasException);
        Assert.True(Contains(end.Exception, previous));
        Assert.True(Contains(end.Exception, resetFailure));
        Assert.Equal(0, ports.Calls);
        Assert.False(context.Cancellation.IsCancellationRequested);
        Assert.False(context.Cancellation.HasTask);
    }

    [Fact]
    public async Task ProductionFlowsKeepFinishingInformationOutOfNormalAndSourceContexts()
    {
        var builder = new ScopeBuilder();
        builder.Apply(new EscapeGameProfile())
            .UseFunction<IWindowOperation, ObservedWindowOperation>()
            .UseFunction<IWindowReaction, ObservedWindowReaction>()
            .UseFunction<IGameplayReaction, ObservedGameplayReaction>()
            .UseFunction<IGameLifetimeReaction, ObservedGameLifetimeReaction>();
        using var scope = builder.BuildScope();
        using var flow = scope.BuildSystemFlow<WindowSystemFlow, IApplicationContext>();
        using var context = new EscapeApplicationContext();

        var end = await flow.ExecuteAsync(context);

        Assert.False(end.HasException);
        Assert.False(context.TryGet<CleanupInfo>(out _));
        var window = Assert.IsType<ObservedWindowReaction>(scope.GetRequiredService<IWindowReaction>());
        var game = Assert.IsType<ObservedGameplayReaction>(scope.GetRequiredService<IGameplayReaction>());
        var session = Assert.IsType<ObservedGameLifetimeReaction>(scope.GetRequiredService<IGameLifetimeReaction>());
        Assert.True(window.Opened);
        Assert.True(window.Closed);
        Assert.True(game.Began);
        Assert.True(session.Ended);
        Assert.False(context.Cancellation.HasTask);
    }

    [Theory]
    [InlineData(CleanupKind.CloseWindow)]
    [InlineData(CleanupKind.EndSession)]
    public async Task WrongFinishingKindDoesNotInvokeOperation(CleanupKind operation)
    {
        using var app = new EscapeApplicationContext();
        using var window = new GameViewContext(app, ContextInheritance.None);
        using var game = new GameplayContext(app, ContextInheritance.Cancellation);
        FlowContext context = operation == CleanupKind.CloseWindow ? window : game;
        var wrong = operation == CleanupKind.CloseWindow ? CleanupKind.EndSession : CleanupKind.CloseWindow;
        context.With(new CleanupInfo(wrong, Result.Success));
        var ports = new FailingPorts(new Exception("must not execute"));

        var end = await ExecuteFinishingAsync(operation, ports, window, game);

        Assert.True(end.HasException);
        Assert.Equal(0, ports.Calls);
        Assert.Contains("matching finishing information", end.Exception!.Message);
    }

    private static Task<InteractionEnd> ExecuteFinishingAsync(
        CleanupKind kind,
        FailingPorts ports,
        IGameViewContext window,
        IGameplayContext game)
        => kind == CleanupKind.CloseWindow
            ? new CloseWindow(ports, new GameExceptionHandling(), new GameCancellationHandling()).ExecuteAsync(window)
            : new EndSession(ports, new GameExceptionHandling(), new GameCancellationHandling()).ExecuteAsync(game);

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

    private sealed class CancelNormally(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort)
        : Interaction<EscapeApplicationContext>(exceptionPort, cancellationPort)
    {
        protected override ValueTask<ReactionEnd> ExecuteCoreAsync(EscapeApplicationContext context)
        {
            context.Cancellation.Cancel();
            return new(Task.FromCanceled<ReactionEnd>(context.Cancellation.GetToken()));
        }
    }

    private sealed class FailingPorts(Exception error) : Reaction, IWindowReaction, IGameLifetimeReaction
    {
        public int Calls { get; private set; }
        public override void ForceResetMemoryState() { }
        public ReactionEnd Open(IGameViewContext context) => GetEnd();
        public ReactionEnd DrawFrame(IGameViewContext context) => GetEnd();
        public ReactionEnd RequestShutdown(ILifetimeContext context) => GetEnd();
        public ReactionEnd Close(ILifetimeContext context, Result previous)
        {
            Calls++;
            throw error;
        }
        public ReactionEnd EndSession(IGameplayContext context, Result previous)
        {
            Calls++;
            throw error;
        }
    }

    private sealed class FailNormally(
        Exception failure,
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort)
        : Interaction<EscapeApplicationContext>(exceptionPort, cancellationPort)
    {
        protected override ValueTask<ReactionEnd> ExecuteCoreAsync(EscapeApplicationContext context)
            => throw failure;
    }

    public sealed class ObservedWindowOperation : Operation, IWindowOperation
    {
        private readonly Stopwatch timeout = Stopwatch.StartNew();
        public override void ForceResetMemoryState() { }
        public void PollFrameInput(IGameViewContext context)
        {
            Assert.False(context.TryGet<CleanupInfo>(out _));
            if (timeout.Elapsed > TimeSpan.FromSeconds(5))
                throw new TimeoutException("Expected session publication.");
            var frame = context.GetRequired<FrameState>();
            frame.Command = null;
            frame.PauseRequested = false;
            frame.SkipRequested = false;
            frame.ShutdownRequested = context.View.CaptureSnapshot().SessionId != 0;
        }
    }

    public sealed class ObservedWindowReaction : Reaction, IWindowReaction
    {
        public bool Opened { get; private set; }
        public bool Closed { get; private set; }
        public override void ForceResetMemoryState() { }
        public ReactionEnd Open(IGameViewContext context)
        {
            Assert.False(context.TryGet<CleanupInfo>(out _));
            Opened = true;
            return GetEnd();
        }
        public ReactionEnd DrawFrame(IGameViewContext context)
        {
            Assert.False(context.TryGet<CleanupInfo>(out _));
            return GetEnd();
        }
        public ReactionEnd Close(ILifetimeContext context, Result previous)
        {
            Assert.Equal(CleanupKind.CloseWindow, context.GetRequired<CleanupInfo>().Kind);
            Closed = true;
            return GetEnd(previous);
        }
    }

    public sealed class ObservedGameLifetimeReaction : Reaction, IGameLifetimeReaction
    {
        public bool Ended { get; private set; }
        public override void ForceResetMemoryState() { }
        public ReactionEnd EndSession(IGameplayContext context, Result previous)
        {
            Assert.Equal(CleanupKind.EndSession, context.GetRequired<CleanupInfo>().Kind);
            context.Gameplay.EndSession(context.SessionId);
            Ended = true;
            return GetEnd(previous);
        }
        public ReactionEnd RequestShutdown(ILifetimeContext context)
        {
            Assert.False(context.TryGet<CleanupInfo>(out _));
            context.Lifetime.RequestShutdown();
            return GetEnd();
        }
    }

    public sealed class ObservedGameplayReaction : Reaction, IGameplayReaction
    {
        private readonly GameplayReaction inner = new();
        public bool Began { get; private set; }
        public override void ForceResetMemoryState() { }
        public ValueTask<ReactionEnd> BeginSessionAsync(IGameplayContext context)
        {
            Assert.False(context.TryGet<CleanupInfo>(out _));
            Began = true;
            return inner.BeginSessionAsync(context);
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
}
