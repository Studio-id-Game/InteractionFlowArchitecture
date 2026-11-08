using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.ExternalPorts;
using InteractionFlow.Samples.Raylib.Externals;
using InteractionFlow.Samples.Raylib.Interactions;
using InteractionFlow.Samples.Raylib.SystemFlows;
using InteractionFlow.Standard.Builders;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.Raylib.Tests;

public sealed class CounterFlowTests
{
    [Fact]
    public async Task ScopeConnectsPortsUsingCounterStateFromContext()
    {
        var builder = new ScopeBuilder();
        builder.UseFunction<ICounterOperation, CounterOperation>()
            .UseFunction<ICounterReaction, CounterReaction>()
            .UseFunction<IExceptionPort<Exception>, CounterExceptionHandling>()
            .UseFunction<ICancellationPort, CounterCancellationHandling>()
            .UseInteraction<IncrementCounter>();
        using var scope = builder.BuildScope();
        using var flow = scope.BuildSystemFlow<CounterSystemFlow, IFlowContext>();
        var operation = scope.GetRequiredService<ICounterOperation>();
        var state = new CounterState();
        using var context = new FlowContext().With(state);

        Assert.Same(state, context.GetRequired<CounterState>());

        var execution = flow.ExecuteAsync(context);
        Assert.True(operation.Submit(context, CounterCommand.Exit));
        Assert.False(
            (await execution.WaitAsync(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken)).HasException);
        state.Snapshot(out _, out var message, out _);
        Assert.Equal("Goodbye", message);
    }

    [Fact]
    public async Task SameOperationKeepsInputWaitsIsolatedByContext()
    {
        var operation = new CounterOperation();
        var firstState = new CounterState();
        var secondState = new CounterState();
        using var firstContext = new FlowContext().With(firstState);
        using var secondContext = new FlowContext().With(secondState);
        var first = operation.ReadAsync(firstContext).AsTask();
        var second = operation.ReadAsync(secondContext).AsTask();

        Assert.True(operation.Submit(firstContext, CounterCommand.Increment));
        Assert.Equal(CounterCommand.Increment, await first.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken));
        Assert.False(second.IsCompleted);
        Assert.False(operation.Submit(firstContext, CounterCommand.Exit));
        Assert.True(operation.Submit(secondContext, CounterCommand.Exit));
        Assert.Equal(CounterCommand.Exit, await second.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RepeatedInputIsNotQueuedWhileReactionRuns()
    {
        var operation = new CounterOperation();
        var state = new CounterState();
        using var context = new FlowContext().With(state);

        var execution = CreateInteraction(operation).ExecuteAsync(context);
        Assert.True(operation.Submit(context, CounterCommand.Increment));
        Assert.False(operation.Submit(context, CounterCommand.Increment));
        var end = await execution.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        Assert.False(end.HasException);
        Assert.Equal(1, state.Count);
        state.Snapshot(out var count, out _, out _);
        Assert.Equal(1, count);
        Assert.False(operation.Submit(context, CounterCommand.Increment));
    }

    [Fact]
    public async Task CancelDuringReactionDoesNotIncrementAndNextInteractionAcceptsInput()
    {
        var operation = new CounterOperation();
        var state = new CounterState();
        using var context = new FlowContext().With(state);
        var interaction = CreateInteraction(operation);

        var execution = interaction.ExecuteAsync(context);
        Assert.True(operation.Submit(context, CounterCommand.Increment));
        await WaitUntilAsync(
            () =>
            {
                state.Snapshot(out _, out var currentMessage, out _);
                return currentMessage.StartsWith("Working", StringComparison.Ordinal);
            });
        context.Cancellation.Cancel();

        var end = await execution.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        Assert.False(end.HasException);
        Assert.Equal(0, state.Count);
        Assert.False(context.Cancellation.IsCancellationRequested);
        state.Snapshot(out _, out _, out var acceptingInput);
        Assert.False(acceptingInput);

        var retry = interaction.ExecuteAsync(context);
        Assert.True(operation.Submit(context, CounterCommand.Increment));
        Assert.False(
            (await retry.WaitAsync(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken)).HasException);
        Assert.Equal(1, state.Count);
    }

    [Fact]
    public async Task CancelInputWaitResumesFlowAndExitCommandEndsIt()
    {
        var operation = new CounterOperation();
        var state = new CounterState();
        using var context = new FlowContext().With(state);

        var execution = new CounterSystemFlow(CreateInteraction(operation)).ExecuteAsync(context);
        context.Cancellation.Cancel();
        await WaitUntilAsync(
            () =>
            {
                state.Snapshot(out _, out var currentMessage, out var currentlyAcceptingInput);
                return currentlyAcceptingInput && currentMessage == "Canceled; ready again";
            });
        Assert.False(execution.IsCompleted);
        Assert.True(operation.Submit(context, CounterCommand.Exit));
        Assert.False(
            (await execution.WaitAsync(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken)).HasException);
        Assert.True(state.ExitRequested);
        state.Snapshot(out _, out var message, out _);
        Assert.Equal("Goodbye", message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WindowCloseIntentSurvivesCancellationReset(bool duringReaction)
    {
        var operation = new CounterOperation();
        var state = new CounterState();
        using var context = new FlowContext().With(state);

        var execution = new CounterSystemFlow(CreateInteraction(operation)).ExecuteAsync(context);

        if (duringReaction)
        {
            Assert.True(operation.Submit(context, CounterCommand.Increment));
            await WaitUntilAsync(
                () =>
                {
                    state.Snapshot(out _, out var currentMessage, out _);
                    return currentMessage.StartsWith("Working", StringComparison.Ordinal);
                });
        }

        state.RequestExit();
        context.Cancellation.Cancel();
        Assert.False(
            (await execution.WaitAsync(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken)).HasException);
        Assert.Equal(0, state.Count);
        state.Snapshot(out _, out _, out var acceptingInput);
        Assert.False(acceptingInput);
        Assert.False(context.Cancellation.IsCancellationRequested);
    }

    private static IncrementCounter CreateInteraction(ICounterOperation operation)
        => new(
            operation,
            new CounterReaction(),
            new CounterExceptionHandling(),
            new CounterCancellationHandling());

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Expected flow state was not reached.");
            await Task.Delay(5, TestContext.Current.CancellationToken);
        }
    }
}
