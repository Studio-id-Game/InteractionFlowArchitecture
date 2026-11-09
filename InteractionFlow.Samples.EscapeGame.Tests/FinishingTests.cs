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

public sealed class FinishingTests
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
        Assert.False(context.TryGet<Finishing>(out _));
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
        Assert.False(context.TryGet<Finishing>(out _));
        Assert.False(context.Cancellation.HasTask);
        Assert.Equal(!failedResult, context.GetRequired<Result>().Try(out _));
    }

    [Theory]
    [InlineData(FinishingKind.CloseWindow, false, false)]
    [InlineData(FinishingKind.CloseWindow, false, true)]
    [InlineData(FinishingKind.CloseWindow, true, false)]
    [InlineData(FinishingKind.CloseWindow, true, true)]
    [InlineData(FinishingKind.EndSession, false, false)]
    [InlineData(FinishingKind.EndSession, false, true)]
    [InlineData(FinishingKind.EndSession, true, false)]
    [InlineData(FinishingKind.EndSession, true, true)]
    public async Task FinishingFailurePreservesOperationAndPriorFailures(
        FinishingKind kind,
        bool canceled,
        bool earlierFailure)
    {
        using var app = new EscapeApplicationContext();
        using var context = app.CreateIndependentContext();
        var previous = new FormatException("earlier failure");
        Exception current = canceled
            ? new OperationCanceledException("finishing canceled")
            : new InvalidOperationException("finishing failed");
        context.With(new Finishing(kind, earlierFailure ? previous : Result.Success));
        var ports = new FailingPorts(current);

        var end = await CreateFinishing(kind, ports).ExecuteAsync(context);

        Assert.True(end.HasException);
        Assert.True(Contains(end.Exception, current));
        if (earlierFailure)
            Assert.True(Contains(end.Exception, previous));
        if (canceled)
            Assert.True(ContainsMessage(end.Exception, "Cancellation is not requested."));
        Assert.Equal(1, ports.Calls);
        Assert.False(app.TryGet<Finishing>(out _));
        Assert.False(context.Cancellation.HasTask);
    }

    [Theory]
    [InlineData(FinishingKind.CloseWindow)]
    [InlineData(FinishingKind.EndSession)]
    public async Task FinishingCancellationRetainsRegisteredTaskFailureDuringReset(FinishingKind kind)
    {
        using var context = new EscapeApplicationContext();
        var previous = new FormatException("prior failure");
        var resetFailure = new InvalidOperationException("registered task failed");
        context.With(new Finishing(kind, previous));
        using var registration = context.Cancellation.AddCancelableTask(Task.FromException(resetFailure));
        context.Cancellation.Cancel();
        var ports = new FailingPorts(new InvalidOperationException("must not execute"));

        var end = await CreateFinishing(kind, ports).ExecuteAsync(context);

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
            .UseFunction<IGameReaction, ObservedGameReaction>()
            .UseFunction<ISessionReaction, ObservedSessionReaction>();
        using var scope = builder.BuildScope();
        using var flow = scope.BuildSystemFlow<WindowSystemFlow, EscapeApplicationContext>();
        using var context = new EscapeApplicationContext();

        var end = await flow.ExecuteAsync(context);

        Assert.False(end.HasException);
        Assert.False(context.TryGet<Finishing>(out _));
        var window = Assert.IsType<ObservedWindowReaction>(scope.GetRequiredService<IWindowReaction>());
        var game = Assert.IsType<ObservedGameReaction>(scope.GetRequiredService<IGameReaction>());
        var session = Assert.IsType<ObservedSessionReaction>(scope.GetRequiredService<ISessionReaction>());
        Assert.True(window.Opened);
        Assert.True(window.Closed);
        Assert.True(game.Began);
        Assert.True(session.Ended);
        Assert.False(context.Cancellation.HasTask);
    }

    [Theory]
    [InlineData(FinishingKind.CloseWindow)]
    [InlineData(FinishingKind.EndSession)]
    public async Task WrongFinishingKindDoesNotInvokeOperation(FinishingKind operation)
    {
        using var context = new EscapeApplicationContext();
        var wrong = operation == FinishingKind.CloseWindow ? FinishingKind.EndSession : FinishingKind.CloseWindow;
        context.With(new Finishing(wrong, Result.Success));
        var ports = new FailingPorts(new Exception("must not execute"));

        var end = await CreateFinishing(operation, ports).ExecuteAsync(context);

        Assert.True(end.HasException);
        Assert.Equal(0, ports.Calls);
        Assert.Contains("matching finishing information", end.Exception!.Message);
    }

    private static IInteraction<EscapeApplicationContext> CreateFinishing(FinishingKind kind, FailingPorts ports)
        => kind == FinishingKind.CloseWindow
            ? new CloseWindow(ports, new GameExceptionHandling(), new GameCancellationHandling())
            : new EndSession(ports, new GameExceptionHandling(), new GameCancellationHandling());

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

    private sealed class FailingPorts(Exception error) : Reaction, IWindowReaction, ISessionReaction
    {
        public int Calls { get; private set; }
        public override void ForceResetMemoryState() { }
        public ReactionEnd Open(EscapeApplicationContext context) => GetEnd();
        public ReactionEnd Draw(EscapeApplicationContext context) => GetEnd();
        public ReactionEnd RequestShutdown(EscapeApplicationContext context) => GetEnd();
        public ReactionEnd Close(EscapeApplicationContext context, Result previous)
        {
            Calls++;
            throw error;
        }
        public ReactionEnd EndSession(EscapeApplicationContext context, Result previous)
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
        public void Poll(EscapeApplicationContext context)
        {
            Assert.False(context.TryGet<Finishing>(out _));
            if (timeout.Elapsed > TimeSpan.FromSeconds(5))
                throw new TimeoutException("Expected session publication.");
            var frame = context.GetRequired<FrameState>();
            frame.Command = null;
            frame.PauseRequested = false;
            frame.SkipRequested = false;
            frame.ShutdownRequested = context.Session.ActiveSessionId != 0;
        }
    }

    public sealed class ObservedWindowReaction : Reaction, IWindowReaction
    {
        public bool Opened { get; private set; }
        public bool Closed { get; private set; }
        public override void ForceResetMemoryState() { }
        public ReactionEnd Open(EscapeApplicationContext context)
        {
            Assert.False(context.TryGet<Finishing>(out _));
            Opened = true;
            return GetEnd();
        }
        public ReactionEnd Draw(EscapeApplicationContext context)
        {
            Assert.False(context.TryGet<Finishing>(out _));
            return GetEnd();
        }
        public ReactionEnd Close(EscapeApplicationContext context, Result previous)
        {
            Assert.Equal(FinishingKind.CloseWindow, context.GetRequired<Finishing>().Kind);
            Closed = true;
            return GetEnd(previous);
        }
    }

    public sealed class ObservedSessionReaction : Reaction, ISessionReaction
    {
        public bool Ended { get; private set; }
        public override void ForceResetMemoryState() { }
        public ReactionEnd EndSession(EscapeApplicationContext context, Result previous)
        {
            Assert.Equal(FinishingKind.EndSession, context.GetRequired<Finishing>().Kind);
            context.Session.EndSession(context.SessionId);
            Ended = true;
            return GetEnd(previous);
        }
        public ReactionEnd RequestShutdown(EscapeApplicationContext context)
        {
            Assert.False(context.TryGet<Finishing>(out _));
            context.Session.RequestShutdown();
            return GetEnd();
        }
    }

    public sealed class ObservedGameReaction : Reaction, IGameReaction
    {
        private readonly GameReaction inner = new();
        public bool Began { get; private set; }
        public override void ForceResetMemoryState() { }
        public ValueTask<ReactionEnd> BeginAsync(EscapeApplicationContext context)
        {
            Assert.False(context.TryGet<Finishing>(out _));
            Began = true;
            return inner.BeginAsync(context);
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
}
