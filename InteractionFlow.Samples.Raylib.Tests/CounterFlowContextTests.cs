using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.Entities.Contexts;
using InteractionFlow.Samples.Raylib.Externals;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.Raylib.Tests;

public sealed class CounterFlowContextTests
{
    [Fact]
    public void RawStateIsUnavailableWhileAccessContractsAreProvided()
    {
        using var context = new CounterFlowContext();

        Assert.NotNull(context.GetRequired<ICounterInputAccess>());
        Assert.NotNull(context.GetRequired<ICounterStateAccess>());
        Assert.NotNull(context.GetRequired<ICounterDisplayAccess>());
        Assert.False(context.TryGet<CounterState>(out _));
        Assert.False(context.TryGet<CounterDisplayState>(out _));
        Assert.False(context.TryGet<CounterInputWait>(out _));
        Assert.False(context.TryGet<CounterAccess>(out _));
    }

    [Fact]
    public void ChildScopeFallsBackToAccessContractsAndRootLocalValues()
    {
        using var root = new CounterFlowContext();
        root.With(new LocalValue("root"));
        using var child = new FlowContext(root).With(new LocalValue("child"));

        Assert.Same(root.GetRequired<ICounterInputAccess>(), child.GetRequired<ICounterInputAccess>());
        Assert.Same(root.GetRequired<ICounterStateAccess>(), child.GetRequired<ICounterStateAccess>());
        Assert.Same(root.GetRequired<ICounterDisplayAccess>(), child.GetRequired<ICounterDisplayAccess>());
        Assert.Same(root.Cancellation, child.Cancellation);
        Assert.Equal("child", child.GetRequired<LocalValue>().Name);
        Assert.Equal("root", root.GetRequired<LocalValue>().Name);
        Assert.False(child.TryGet<CounterState>(out _));
    }

    [Fact]
    public async Task IndependentContextSharesAccessButNotCancellationOrLocalValues()
    {
        using var root = new CounterFlowContext();
        root.With(new LocalValue("root"));
        using var independent = root.CreateIndependentContext();

        Assert.Same(root.GetRequired<ICounterInputAccess>(), independent.GetRequired<ICounterInputAccess>());
        Assert.Same(root.GetRequired<ICounterStateAccess>(), independent.GetRequired<ICounterStateAccess>());
        Assert.Same(root.GetRequired<ICounterDisplayAccess>(), independent.GetRequired<ICounterDisplayAccess>());
        Assert.NotSame(root.Cancellation, independent.Cancellation);
        Assert.False(independent.TryGet<LocalValue>(out _));
        Assert.False(independent.TryGet<CounterState>(out _));

        root.Cancellation.Cancel();
        Assert.True(root.Cancellation.IsCancellationRequested);
        Assert.False(independent.Cancellation.IsCancellationRequested);
        var input = independent.GetRequired<ICounterInputAccess>();
        var read = input.ReadCommandAsync(independent.Cancellation.GetToken());
        Assert.True(input.TrySubmit(CounterCommand.Increment));
        Assert.Equal(CounterCommand.Increment, await read);
        var state = root.GetRequired<ICounterStateAccess>();
        Assert.True(state.BeginIncrement());
        Assert.True(state.CompleteIncrement());
        independent.GetRequired<ICounterDisplayAccess>().Snapshot(out var count, out _, out _);
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposingOneContextDoesNotDisposeSharedStateOrOtherContext(bool disposeRoot)
    {
        using var root = new CounterFlowContext();
        using var independent = root.CreateIndependentContext();
        var survivor = disposeRoot ? independent : root;
        var discarded = disposeRoot ? root : independent;
        var input = survivor.GetRequired<ICounterInputAccess>();
        var read = input.ReadCommandAsync(survivor.Cancellation.GetToken());

        discarded.Dispose();

        Assert.Same(input, survivor.GetRequired<ICounterInputAccess>());
        Assert.False(read.IsCompleted);
        Assert.True(input.TrySubmit(CounterCommand.Increment));
        Assert.Equal(CounterCommand.Increment, await read);
        var state = survivor.GetRequired<ICounterStateAccess>();
        Assert.True(state.BeginIncrement());
        Assert.True(state.CompleteIncrement());
        Assert.Equal(1, state.Count);
    }

    [Fact]
    public void DisposedContextCannotCreateSiblingButPreviouslyAcquiredAccessRemainsUsable()
    {
        using var context = new CounterFlowContext();
        var state = context.GetRequired<ICounterStateAccess>();
        var display = context.GetRequired<ICounterDisplayAccess>();

        context.Dispose();

        Assert.Throws<ObjectDisposedException>(() => context.CreateIndependentContext());
        Assert.Throws<ObjectDisposedException>(() => context.GetRequired<ICounterStateAccess>());
        state.RequestExit();
        Assert.True(state.ExitRequested);
        display.Snapshot(out var count, out var message, out var acceptingInput);
        Assert.Equal(0, count);
        Assert.Equal("Goodbye", message);
        Assert.False(acceptingInput);
    }

    [Fact]
    public async Task ReactionRechecksSharedExitAfterAsynchronousDelay()
    {
        using var context = new CounterFlowContext();
        using var independent = context.CreateIndependentContext();
        var input = context.GetRequired<ICounterInputAccess>();
        var read = input.ReadCommandAsync(context.Cancellation.GetToken());
        Assert.True(input.TrySubmit(CounterCommand.Increment));
        Assert.Equal(CounterCommand.Increment, await read);
        var reaction = new CounterReaction();

        var execution = reaction.RespondAsync(context, CounterCommand.Increment).AsTask();
        context.GetRequired<ICounterDisplayAccess>().Snapshot(out _, out var workingMessage, out _);
        Assert.StartsWith("Working", workingMessage);
        independent.GetRequired<ICounterStateAccess>().RequestExit();
        Assert.False((await execution.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken)).HasException);

        var state = context.GetRequired<ICounterStateAccess>();
        Assert.True(state.ExitRequested);
        Assert.Equal(0, state.Count);
        context.GetRequired<ICounterDisplayAccess>().Snapshot(out var count, out var message, out var acceptingInput);
        Assert.Equal(0, count);
        Assert.Equal("Goodbye", message);
        Assert.False(acceptingInput);
    }

    private sealed record LocalValue(string Name);
}
