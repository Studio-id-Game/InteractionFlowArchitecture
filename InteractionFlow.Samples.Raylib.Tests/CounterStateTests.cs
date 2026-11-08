using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.Entities.Contexts;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.Raylib.Tests;

public sealed class CounterStateTests
{
    [Fact]
    public async Task ContextsShareStateAndCapturedValuesRemainUnchangedAfterUpdates()
    {
        using var root = new CounterFlowContext();
        using var child = new FlowContext(root);
        var state = root.GetRequired<ICounterStateAccess>();
        var input = root.GetRequired<ICounterInputAccess>();
        var display = root.GetRequired<ICounterDisplayAccess>();

        Assert.Same(state, root.GetRequired<ICounterStateAccess>());
        Assert.Same(state, child.GetRequired<ICounterStateAccess>());
        display.Snapshot(out var initialCount, out var initialMessage, out var initiallyAcceptingInput);

        var read = child.GetRequired<ICounterInputAccess>().ReadCommandAsync(TestContext.Current.CancellationToken);
        Assert.True(input.TrySubmit(CounterCommand.Increment));
        Assert.Equal(CounterCommand.Increment, await read);
        Assert.True(state.BeginIncrement());
        Assert.True(state.CompleteIncrement());

        Assert.Equal(0, initialCount);
        Assert.Equal("Ready", initialMessage);
        Assert.False(initiallyAcceptingInput);

        root.GetRequired<ICounterDisplayAccess>().Snapshot(out var count, out var message, out var acceptingInput);
        Assert.Equal(1, count);
        Assert.Equal("Completed", message);
        Assert.False(acceptingInput);
    }

    [Fact]
    public async Task AcceptingInputMatchesPendingReadAndBusyCommandsAreNotQueued()
    {
        using var context = new CounterFlowContext();
        var state = context.GetRequired<ICounterStateAccess>();
        var input = context.GetRequired<ICounterInputAccess>();
        var display = context.GetRequired<ICounterDisplayAccess>();
        Assert.False(input.TrySubmit(CounterCommand.Increment));
        display.Snapshot(out _, out _, out var beforeRead);
        Assert.False(beforeRead);

        var read = input.ReadCommandAsync(TestContext.Current.CancellationToken);
        display.Snapshot(out _, out _, out var whileWaiting);
        Assert.True(whileWaiting);
        Assert.False(read.IsCompleted);
        Assert.True(input.TrySubmit(CounterCommand.Increment));
        Assert.False(input.TrySubmit(CounterCommand.Increment));
        Assert.Equal(CounterCommand.Increment, await read);

        Assert.True(state.BeginIncrement());
        display.Snapshot(out var workingCount, out var workingMessage, out var whileWorking);
        Assert.Equal(0, workingCount);
        Assert.StartsWith("Working", workingMessage);
        Assert.False(whileWorking);
        Assert.False(input.TrySubmit(CounterCommand.Exit));
        Assert.True(state.CompleteIncrement());

        var nextRead = input.ReadCommandAsync(TestContext.Current.CancellationToken);
        Assert.False(nextRead.IsCompleted);
        Assert.True(input.TrySubmit(CounterCommand.Exit));
        Assert.Equal(CounterCommand.Exit, await nextRead);
    }

    [Fact]
    public async Task CanceledReadCanResumeAfterCancelTransition()
    {
        using var context = new CounterFlowContext();
        var state = context.GetRequired<ICounterStateAccess>();
        var input = context.GetRequired<ICounterInputAccess>();
        var display = context.GetRequired<ICounterDisplayAccess>();
        using var source = new CancellationTokenSource();
        var read = input.ReadCommandAsync(source.Token).AsTask();
        source.Cancel();
        display.Snapshot(out _, out _, out var canceledAcceptance);
        Assert.False(canceledAcceptance);
        Assert.False(input.TrySubmit(CounterCommand.Increment));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await read);

        state.Cancel();
        Assert.False(state.ExitRequested);
        var retry = input.ReadCommandAsync(TestContext.Current.CancellationToken);
        display.Snapshot(out _, out var message, out var acceptingInput);
        Assert.Equal("Canceled; ready again", message);
        Assert.True(acceptingInput);
        Assert.True(input.TrySubmit(CounterCommand.Increment));
        Assert.Equal(CounterCommand.Increment, await retry);
    }

    [Fact]
    public async Task ExitDuringWorkCannotBeReopenedByCompletionCancellationOrRead()
    {
        using var context = new CounterFlowContext();
        var state = context.GetRequired<ICounterStateAccess>();
        var input = context.GetRequired<ICounterInputAccess>();
        var display = context.GetRequired<ICounterDisplayAccess>();
        var read = input.ReadCommandAsync(TestContext.Current.CancellationToken);
        Assert.True(input.TrySubmit(CounterCommand.Increment));
        Assert.Equal(CounterCommand.Increment, await read);
        Assert.True(state.BeginIncrement());

        state.RequestExit();
        Assert.False(state.CompleteIncrement());
        state.Cancel();
        Assert.False(state.BeginIncrement());
        Assert.True(state.ExitRequested);
        Assert.Equal(0, state.Count);
        Assert.Equal(CounterCommand.Exit, await input.ReadCommandAsync(TestContext.Current.CancellationToken));
        Assert.False(input.TrySubmit(CounterCommand.Increment));
        display.Snapshot(out _, out _, out var acceptingInput);
        Assert.False(acceptingInput);
    }

    [Fact]
    public async Task ExitClosesAcceptanceWhileExistingReadWaitsForOwnerCancellation()
    {
        using var context = new CounterFlowContext();
        var state = context.GetRequired<ICounterStateAccess>();
        var input = context.GetRequired<ICounterInputAccess>();
        var display = context.GetRequired<ICounterDisplayAccess>();
        using var source = new CancellationTokenSource();
        var read = input.ReadCommandAsync(source.Token).AsTask();

        state.RequestExit();
        display.Snapshot(out _, out _, out var acceptingInput);
        Assert.False(acceptingInput);
        Assert.False(input.TrySubmit(CounterCommand.Increment));
        Assert.False(read.IsCompleted);
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await read);

        state.Cancel();
        Assert.True(state.ExitRequested);
        Assert.Equal(CounterCommand.Exit, await input.ReadCommandAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void FailureMessageSurvivesExitAndLateCancellation()
    {
        using var context = new CounterFlowContext();
        var state = context.GetRequired<ICounterStateAccess>();
        var display = context.GetRequired<ICounterDisplayAccess>();
        state.Fail(new InvalidOperationException("reaction failed"));
        display.Snapshot(out _, out var failureMessage, out var acceptingInput);
        Assert.Contains("reaction failed", failureMessage);
        Assert.False(acceptingInput);

        state.RequestExit();
        state.Cancel();
        Assert.False(state.CompleteIncrement());
        display.Snapshot(out var count, out var finalMessage, out var finallyAcceptingInput);
        Assert.Equal(0, count);
        Assert.Equal(failureMessage, finalMessage);
        Assert.False(finallyAcceptingInput);
        Assert.True(state.ExitRequested);
    }

    [Fact]
    public async Task SnapshotsRemainConsistentDuringConcurrentIncrementTransitions()
    {
        using var context = new CounterFlowContext();
        var state = context.GetRequired<ICounterStateAccess>();
        var input = context.GetRequired<ICounterInputAccess>();
        var display = context.GetRequired<ICounterDisplayAccess>();
        var token = TestContext.Current.CancellationToken;
        var processing = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueProcessing = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var writer = Task.Run(async () =>
        {
            for (var i = 0; i < 256; i++)
            {
                var read = input.ReadCommandAsync(token);
                Assert.True(input.TrySubmit(CounterCommand.Increment));
                Assert.Equal(CounterCommand.Increment, await read);
                Assert.True(state.BeginIncrement());
                if (i == 0)
                {
                    processing.SetResult(true);
                    await continueProcessing.Task;
                }
                Assert.True(state.CompleteIncrement());
            }
        }, token);

        await processing.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
        try
        {
            display.Snapshot(out var workingCount, out var workingMessage, out var acceptingWhileWorking);
            Assert.Equal(0, workingCount);
            Assert.StartsWith("Working", workingMessage);
            Assert.False(acceptingWhileWorking);
        }
        finally
        {
            continueProcessing.SetResult(true);
        }

        while (!writer.IsCompleted)
        {
            display.Snapshot(out var count, out var message, out var acceptingInput);
            Assert.InRange(count, 0, 256);
            if (message.StartsWith("Working", StringComparison.Ordinal))
            {
                Assert.InRange(count, 0, 255);
                Assert.False(acceptingInput);
            }
            else if (message == "Completed")
                Assert.InRange(count, 1, 256);
            else
                Assert.Equal("Ready", message);
            await Task.Yield();
        }

        await writer;
        display.Snapshot(out var finalCount, out var finalMessage, out var finallyAcceptingInput);
        Assert.Equal(256, finalCount);
        Assert.Equal("Completed", finalMessage);
        Assert.False(finallyAcceptingInput);
    }
}
