using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Raylib.Entities;
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
        var state = new CounterState();
        using var root = new FlowContext().With(state);
        using var child = new FlowContext(root);

        Assert.Same(state, root.GetRequired<CounterState>());
        Assert.Same(state, child.GetRequired<CounterState>());
        state.Snapshot(out var initialCount, out var initialMessage, out var initiallyAcceptingInput);

        var read = child.GetRequired<CounterState>().ReadCommandAsync(TestContext.Current.CancellationToken);
        Assert.True(state.TrySubmit(CounterCommand.Increment));
        Assert.Equal(CounterCommand.Increment, await read);
        Assert.True(state.BeginIncrement());
        Assert.True(state.CompleteIncrement());

        Assert.Equal(0, initialCount);
        Assert.Equal("Ready", initialMessage);
        Assert.False(initiallyAcceptingInput);

        root.GetRequired<CounterState>().Snapshot(out var count, out var message, out var acceptingInput);
        Assert.Equal(1, count);
        Assert.Equal("Completed", message);
        Assert.False(acceptingInput);
    }

    [Fact]
    public async Task AcceptingInputMatchesPendingReadAndBusyCommandsAreNotQueued()
    {
        var state = new CounterState();
        Assert.False(state.TrySubmit(CounterCommand.Increment));
        state.Snapshot(out _, out _, out var beforeRead);
        Assert.False(beforeRead);

        var read = state.ReadCommandAsync(TestContext.Current.CancellationToken);
        state.Snapshot(out _, out _, out var whileWaiting);
        Assert.True(whileWaiting);
        Assert.False(read.IsCompleted);
        Assert.True(state.TrySubmit(CounterCommand.Increment));
        Assert.False(state.TrySubmit(CounterCommand.Increment));
        Assert.Equal(CounterCommand.Increment, await read);

        Assert.True(state.BeginIncrement());
        state.Snapshot(out var workingCount, out var workingMessage, out var whileWorking);
        Assert.Equal(0, workingCount);
        Assert.StartsWith("Working", workingMessage);
        Assert.False(whileWorking);
        Assert.False(state.TrySubmit(CounterCommand.Exit));
        Assert.True(state.CompleteIncrement());

        var nextRead = state.ReadCommandAsync(TestContext.Current.CancellationToken);
        Assert.False(nextRead.IsCompleted);
        Assert.True(state.TrySubmit(CounterCommand.Exit));
        Assert.Equal(CounterCommand.Exit, await nextRead);
    }

    [Fact]
    public async Task CanceledReadCanResumeAfterCancelTransition()
    {
        var state = new CounterState();
        using var source = new CancellationTokenSource();
        var read = state.ReadCommandAsync(source.Token).AsTask();
        source.Cancel();
        state.Snapshot(out _, out _, out var canceledAcceptance);
        Assert.False(canceledAcceptance);
        Assert.False(state.TrySubmit(CounterCommand.Increment));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await read);

        state.Cancel();
        Assert.False(state.ExitRequested);
        var retry = state.ReadCommandAsync(TestContext.Current.CancellationToken);
        state.Snapshot(out _, out var message, out var acceptingInput);
        Assert.Equal("Canceled; ready again", message);
        Assert.True(acceptingInput);
        Assert.True(state.TrySubmit(CounterCommand.Increment));
        Assert.Equal(CounterCommand.Increment, await retry);
    }

    [Fact]
    public async Task ExitDuringWorkCannotBeReopenedByCompletionCancellationOrRead()
    {
        var state = new CounterState();
        var read = state.ReadCommandAsync(TestContext.Current.CancellationToken);
        Assert.True(state.TrySubmit(CounterCommand.Increment));
        Assert.Equal(CounterCommand.Increment, await read);
        Assert.True(state.BeginIncrement());

        state.RequestExit();
        Assert.False(state.CompleteIncrement());
        state.Cancel();
        Assert.False(state.BeginIncrement());
        Assert.True(state.ExitRequested);
        Assert.Equal(0, state.Count);
        Assert.Equal(CounterCommand.Exit, await state.ReadCommandAsync(TestContext.Current.CancellationToken));
        Assert.False(state.TrySubmit(CounterCommand.Increment));
        state.Snapshot(out _, out _, out var acceptingInput);
        Assert.False(acceptingInput);
    }

    [Fact]
    public async Task ExitClosesAcceptanceWhileExistingReadWaitsForOwnerCancellation()
    {
        var state = new CounterState();
        using var source = new CancellationTokenSource();
        var read = state.ReadCommandAsync(source.Token).AsTask();

        state.RequestExit();
        state.Snapshot(out _, out _, out var acceptingInput);
        Assert.False(acceptingInput);
        Assert.False(state.TrySubmit(CounterCommand.Increment));
        Assert.False(read.IsCompleted);
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await read);

        state.Cancel();
        Assert.True(state.ExitRequested);
        Assert.Equal(CounterCommand.Exit, await state.ReadCommandAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void FailureMessageSurvivesExitAndLateCancellation()
    {
        var state = new CounterState();
        state.Fail(new InvalidOperationException("reaction failed"));
        state.Snapshot(out _, out var failureMessage, out var acceptingInput);
        Assert.Contains("reaction failed", failureMessage);
        Assert.False(acceptingInput);

        state.RequestExit();
        state.Cancel();
        Assert.False(state.CompleteIncrement());
        state.Snapshot(out var count, out var finalMessage, out var finallyAcceptingInput);
        Assert.Equal(0, count);
        Assert.Equal(failureMessage, finalMessage);
        Assert.False(finallyAcceptingInput);
        Assert.True(state.ExitRequested);
    }

    [Fact]
    public async Task SnapshotsRemainConsistentDuringConcurrentIncrementTransitions()
    {
        var state = new CounterState();
        var token = TestContext.Current.CancellationToken;
        var processing = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueProcessing = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var writer = Task.Run(async () =>
        {
            for (var i = 0; i < 256; i++)
            {
                var read = state.ReadCommandAsync(token);
                Assert.True(state.TrySubmit(CounterCommand.Increment));
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
            state.Snapshot(out var workingCount, out var workingMessage, out var acceptingWhileWorking);
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
            state.Snapshot(out var count, out var message, out var acceptingInput);
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
        state.Snapshot(out var finalCount, out var finalMessage, out var finallyAcceptingInput);
        Assert.Equal(256, finalCount);
        Assert.Equal("Completed", finalMessage);
        Assert.False(finallyAcceptingInput);
    }
}
