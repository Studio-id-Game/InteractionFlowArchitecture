using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.ExternalPorts;
using InteractionFlow.Samples.Raylib.Externals;
using InteractionFlow.Samples.Raylib.Interactions;
using InteractionFlow.Samples.Raylib.SystemFlows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.Raylib.Tests;

[Collection("Window console output")]
public sealed class WindowFlowTests
{
    [Fact]
    public async Task FramesUseOpeningThreadAndIndependentCancellation()
    {
        var display = new CounterDisplayState();
        var operation = new CounterOperation();
        var state = new CounterState();
        using var context = new FlowContext().With(state).With(display);
        var owner = Environment.CurrentManagedThreadId;
        var frames = 0;
        var window = new TestWindow();
        var input = new TestFrame(frame =>
        {
            Assert.Equal(owner, Environment.CurrentManagedThreadId);
            Assert.NotSame(context.Cancellation, frame.Cancellation);
            Assert.Same(display, frame.GetRequired<CounterDisplayState>());
            frames++;
            frame.GetRequired<FrameState>().CloseRequested = true;
            frame.GetRequired<CounterState>().ExitRequested = true;
        });

        var flow = CreateFlow(operation, new CounterReaction(), window, input);
        var execution = flow.ExecuteAsync(context);
        Assert.True(execution.IsCompletedSuccessfully);
        Assert.False((await execution).HasException);
        Assert.Equal(1, frames);
        Assert.Equal(new[] { "open", "draw", "close" }, window.Events);
        Assert.All(window.Threads, thread => Assert.Equal(owner, thread));
        Assert.False(context.Cancellation.HasTask);
    }

    [Fact]
    public async Task CancelInputWaitResetsWorkerWhileFramesContinueAndExitEndsSession()
    {
        var display = new CounterDisplayState();
        var operation = new CounterOperation();
        var phase = 0;
        var timeout = DateTime.UtcNow.AddSeconds(5);
        var input = new TestFrame(frame =>
        {
            Assert.True(DateTime.UtcNow < timeout, "Cancel/retry did not complete.");
            Assert.False(frame.Cancellation.IsCancellationRequested);
            display.Snapshot(out _, out var currentMessage, out var acceptingInput);
            var cancel = phase == 0 && acceptingInput;
            if (cancel)
                phase = 1;
            else if (phase == 1 && acceptingInput && currentMessage == "Canceled; ready again")
            {
                frame.GetRequired<FrameState>().Command = CounterCommand.Exit;
                phase = 2;
            }
            frame.GetRequired<FrameState>().CancelRequested = cancel;
            Thread.Sleep(1);
        });

        using var context = new FlowContext().With(new CounterState()).With(display);
        var window = new TestWindow();
        var end = await CreateFlow(operation, new CounterReaction(), window, input)
            .ExecuteAsync(context);

        Assert.False(end.HasException);
        Assert.Equal(2, phase);
        display.Snapshot(out _, out var message, out _);
        Assert.Equal("Goodbye", message);
        Assert.True(window.Events.FindAll(name => name == "draw").Count > 1);
    }

    [Fact]
    public async Task WindowCloseWaitsForCanceledWorkerCleanupBeforeClosing()
    {
        var display = new CounterDisplayState();
        var operation = new CounterOperation();
        var worker = new DelayedCounterReaction();
        var window = new TestWindow { OnClose = () => Assert.True(worker.Finished) };
        var timeout = DateTime.UtcNow.AddSeconds(5);
        var input = new TestFrame(frame =>
        {
            Assert.True(DateTime.UtcNow < timeout, "Worker did not start.");
            frame.GetRequired<FrameState>().Command = CounterCommand.Increment;
            var close = worker.Started;
            frame.GetRequired<FrameState>().CloseRequested = close;
            if (close)
                frame.GetRequired<CounterState>().ExitRequested = true;
            Thread.Sleep(1);
        });

        using var context = new FlowContext().With(new CounterState()).With(display);
        var end = await CreateFlow(operation, worker, window, input).ExecuteAsync(context);

        Assert.False(end.HasException);
        Assert.True(worker.Finished);
        Assert.Equal("close", window.Events[^1]);
        Assert.False(context.Cancellation.HasTask);
    }

    [Theory]
    [InlineData("open")]
    [InlineData("frame")]
    [InlineData("worker")]
    public async Task FailureStillClosesWindowAndPreservesCause(string phase)
    {
        var failure = new InvalidOperationException(phase + " failed");
        var display = new CounterDisplayState();
        var operation = new CounterOperation();
        var window = new TestWindow { OpenFailure = phase == "open" ? failure : null };
        var input = new TestFrame(frame =>
        {
            if (phase == "frame")
                throw failure;
            frame.GetRequired<FrameState>().Command = CounterCommand.Increment;
            Thread.Sleep(1);
        });
        ICounterReaction worker = phase == "worker"
            ? new FailingCounterReaction(failure)
            : new CounterReaction();
        using var context = new FlowContext().With(new CounterState()).With(display);
        var end = await CreateFlow(operation, worker, window, input).ExecuteAsync(context);

        Assert.True(end.HasException);
        Assert.True(Contains(end.Exception, failure));
        Assert.Equal("close", window.Events[^1]);
        Assert.False(context.Cancellation.HasTask);
    }

    [Fact]
    public void SuccessfulClosePreservesEarlierFailure()
    {
        var previous = new InvalidOperationException("frame failed");
        var reaction = new RaylibWindowReaction();
        using var context = new FlowContext().With(new WindowState());
        var end = reaction.Close(context, previous);
        Assert.True(Contains(end.Exception, previous));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CloseFailureIsAggregatedWithEarlierFrameFailure(bool throwFailure)
    {
        var previous = new InvalidOperationException("frame failed");
        var closing = new InvalidOperationException("close failed");
        var display = new CounterDisplayState();
        var operation = new CounterOperation();
        var window = new TestWindow { CloseFailure = closing, ThrowCloseFailure = throwFailure };
        var input = new TestFrame(_ => throw previous);
        using var context = new FlowContext().With(new CounterState()).With(display);
        var end = await CreateFlow(operation, new CounterReaction(), window, input)
            .ExecuteAsync(context);

        Assert.True(Contains(end.Exception, previous));
        Assert.True(Contains(end.Exception, closing));
        Assert.Equal("close", window.Events[^1]);
    }

    [Fact]
    public void CloseNotificationFailurePreservesEarlierFailure()
    {
        var previous = new InvalidOperationException("frame failed");
        var reporting = new InvalidOperationException("notification failed");
        var reaction = new RaylibWindowReaction();
        using var context = new FlowContext().With(new WindowState());
        var original = Console.Error;
        using var output = new ThrowingWriter(reporting);
        try
        {
            Console.SetError(output);
            var end = reaction.Close(context, previous);
            Assert.True(Contains(end.Exception, previous));
            Assert.True(Contains(end.Exception, reporting));
        }
        finally
        {
            Console.SetError(original);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CloseCancellationRemainsFailureAndPreservesEarlierResult(bool earlierFailure)
    {
        var previous = new InvalidOperationException("frame failed");
        var canceled = new OperationCanceledException("close canceled");
        var display = new CounterDisplayState();
        var operation = new CounterOperation();
        var window = new TestWindow { CloseFailure = canceled, ThrowCloseFailure = true };
        var input = new TestFrame(frame =>
        {
            if (earlierFailure)
                throw previous;
            frame.GetRequired<FrameState>().CloseRequested = true;
            frame.GetRequired<CounterState>().ExitRequested = true;
        });

        using var context = new FlowContext().With(new CounterState()).With(display);
        var end = await CreateFlow(operation, new CounterReaction(), window, input)
            .ExecuteAsync(context);

        Assert.True(end.HasException);
        Assert.True(Contains(end.Exception, canceled));
        if (earlierFailure)
            Assert.True(Contains(end.Exception, previous));
        Assert.Equal("close", window.Events[^1]);
        Assert.False(context.Cancellation.HasTask);
    }

    [Fact]
    public async Task ClosingUnopenedWindowIsSuccessfulAndRepeatable()
    {
        var window = new WindowState();
        var display = new CounterDisplayState();
        var reaction = new RaylibWindowReaction();
        var interaction = new CloseWindow(
            reaction,
            new CounterExceptionHandling(),
            new CounterCancellationHandling());
        using var context = new FlowContext()
            .With(new CounterState())
            .With(display)
            .With(window)
            .With<Result>(Result.Success);
        var end = await interaction.ExecuteAsync(context);

        Assert.False(end.HasException);
        Assert.False((await interaction.ExecuteAsync(context)).HasException);
        Assert.False(window.IsOpen);
    }

    [Fact]
    public void WindowStateChecksOpenStateAndOpeningThread()
    {
        var window = new WindowState();
        Assert.Throws<InvalidOperationException>(window.VerifyAccess);
        window.MarkOpened();
        window.VerifyAccess();
        Assert.Throws<InvalidOperationException>(window.MarkOpened);

        Exception? error = null;
        var otherThread = new Thread(() => error = Record.Exception(window.VerifyAccess));
        otherThread.Start();
        otherThread.Join();
        Assert.IsType<InvalidOperationException>(error);

        window.MarkClosed();
        Assert.False(window.IsOpen);
        Assert.Throws<InvalidOperationException>(window.VerifyAccess);
    }

    private static WindowSystemFlow CreateFlow(
        ICounterOperation operation,
        ICounterReaction counter,
        IWindowReaction window,
        IWindowOperation input)
    {
        var exception = new CounterExceptionHandling();
        var cancellation = new CounterCancellationHandling();

        return new(
            new OpenWindow(window, exception, cancellation),
            new PresentFrame(input, window, operation, exception, cancellation),
            new CloseWindow(window, exception, cancellation),
            new CounterSystemFlow(new IncrementCounter(operation, counter, exception, cancellation)));
    }

    private static bool Contains(Exception? actual, Exception expected)
    {
        if (ReferenceEquals(actual, expected))
            return true;

        if (actual is AggregateException aggregate)
            foreach (var inner in aggregate.InnerExceptions)
                if (Contains(inner, expected))
                    return true;

        return actual?.InnerException is not null && Contains(actual.InnerException, expected);
    }

    private sealed class TestFrame(Action<IFlowContext> poll) : Operation, IWindowOperation
    {
        public override void ForceResetMemoryState()
        {
        }

        public void Poll(IFlowContext context)
        {
            context.GetRequired<FrameState>().Command = null;
            poll(context);
        }
    }

    private sealed class TestWindow : Reaction, IWindowReaction
    {
        public List<string> Events { get; } = [];
        public List<int> Threads { get; } = [];
        public Exception? OpenFailure { get; init; }
        public Exception? CloseFailure { get; init; }
        public bool ThrowCloseFailure { get; init; }
        public Action? OnClose { get; init; }

        public override void ForceResetMemoryState()
        {
        }

        public ReactionEnd Open(IFlowContext context)
        {
            Record("open");
            return GetEnd(OpenFailure);
        }

        public ReactionEnd Draw(IFlowContext context)
        {
            Record("draw");
            return GetEnd();
        }

        public ReactionEnd Close(IFlowContext context, Result result)
        {
            Record("close");
            OnClose?.Invoke();
            if (ThrowCloseFailure && CloseFailure is not null)
                throw CloseFailure;

            if (CloseFailure is not null)
                result = result.Try(out var previous)
                    ? CloseFailure
                    : new AggregateException(previous, CloseFailure);

            return GetEnd(result);
        }

        private void Record(string name)
        {
            Events.Add(name);
            Threads.Add(Environment.CurrentManagedThreadId);
        }
    }

    private sealed class ThrowingWriter(Exception error) : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(object? value) => throw error;
        public override void WriteLine(string? value) => throw error;
    }

    private sealed class DelayedCounterReaction : Reaction, ICounterReaction
    {
        public volatile bool Started;
        public volatile bool Finished;

        public override void ForceResetMemoryState()
        {
        }

        public async ValueTask<ReactionEnd> RespondAsync(IFlowContext context, CounterCommand command)
        {
            Started = true;

            try
            {
                await Task.Delay(Timeout.Infinite, context.Cancellation.GetToken());
            }
            finally
            {
                await Task.Delay(30, TestContext.Current.CancellationToken);
                Finished = true;
            }

            return GetEnd();
        }
    }

    private sealed class FailingCounterReaction(Exception failure) : Reaction, ICounterReaction
    {
        public override void ForceResetMemoryState()
        {
        }

        public ValueTask<ReactionEnd> RespondAsync(IFlowContext context, CounterCommand command)
            => new(GetEnd(failure));
    }

}

[CollectionDefinition("Window console output", DisableParallelization = true)]
public sealed class WindowConsoleOutputCollection
{
}
