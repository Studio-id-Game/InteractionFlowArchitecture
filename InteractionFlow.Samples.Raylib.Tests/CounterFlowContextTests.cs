using InteractionFlow.Core.Entities;
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
    public void FixedPropertiesMatchLookupOnTypedParentChildAndIndependentContexts()
    {
        using var parent = new CounterFlowContext();
        parent.With(new LocalValue("parent"));
        using var child = new CounterFlowContext(parent);
        child.With(new LocalValue("child"));
        using var independent = parent.CreateIndependentContext();

        Assert.Same(parent.Input, parent.GetRequired<ICounterInputAccess>());
        Assert.Same(parent.State, parent.GetRequired<ICounterStateAccess>());
        Assert.Same(parent.Display, parent.GetRequired<ICounterDisplayAccess>());
        Assert.Same(parent.Input, child.Input);
        Assert.Same(parent.State, child.State);
        Assert.Same(parent.Display, child.Display);
        Assert.Same(child.Input, child.GetRequired<ICounterInputAccess>());
        Assert.Same(child.State, child.GetRequired<ICounterStateAccess>());
        Assert.Same(child.Display, child.GetRequired<ICounterDisplayAccess>());
        Assert.Same(parent.Cancellation, child.Cancellation);
        Assert.Equal("child", child.GetRequired<LocalValue>().Name);
        Assert.Equal("parent", parent.GetRequired<LocalValue>().Name);
        Assert.Same(parent.Input, independent.Input);
        Assert.NotSame(parent.Cancellation, independent.Cancellation);
        Assert.False(independent.TryGet<LocalValue>(out _));
    }

    [Theory]
    [InlineData("input")]
    [InlineData("state")]
    [InlineData("display")]
    public void RuntimeAccessValuesCannotOverrideFixedPropertiesThroughObjectRegistration(string contract)
    {
        using var context = new CounterFlowContext();
        using var replacement = new CounterFlowContext();
        var originalInput = context.Input;
        var originalState = context.State;
        var originalDisplay = context.Display;
        object value = contract switch
        {
            "input" => replacement.Input,
            "state" => replacement.State,
            _ => replacement.Display
        };

        Assert.Throws<ArgumentException>(() => context.With<object>(value));

        Assert.Same(originalInput, context.Input);
        Assert.Same(originalState, context.State);
        Assert.Same(originalDisplay, context.Display);
        Assert.Same(context.Input, context.GetRequired<ICounterInputAccess>());
        Assert.Same(context.State, context.GetRequired<ICounterStateAccess>());
        Assert.Same(context.Display, context.GetRequired<ICounterDisplayAccess>());
    }

    [Fact]
    public void TypedChildRejectsFixedAccessAndAllEntriesWhileOrdinaryValuesRemainAllowed()
    {
        using var parent = new CounterFlowContext();
        using var child = new CounterFlowContext(parent);
        using var textEntry = new RefEntry<string>("ordinary text");
        using var mutableEntry = new RefEntry<object>(null);
        using var customEntry = new CustomEntry("custom");
        using var derivedEntry = new DerivedEntry();

        Assert.Throws<ArgumentException>(() => child.With<ICounterInputAccess>(parent.Input));
        Assert.Throws<ArgumentException>(() => child.With(textEntry));
        Assert.Throws<ArgumentException>(() => child.With<object>(mutableEntry));
        Assert.Throws<ArgumentException>(() => child.With<object>(customEntry));
        Assert.Throws<ArgumentException>(() => child.With<object>(derivedEntry));
        var frames = new FrameState();
        var window = new WindowState();
        child.With(frames).With(window).With<Result>(Result.Success);

        Assert.Same(frames, child.GetRequired<FrameState>());
        Assert.Same(window, child.GetRequired<WindowState>());
        Assert.True(child.GetRequired<Result>().Try(out _));
        Assert.False(child.TryGet<RefEntry<string>>(out _));
        Assert.False(child.TryGet<RefEntry<object>>(out _));
    }

    [Fact]
    public void OrdinaryChildMayOverrideParentAccessIncludingThroughMutableEntry()
    {
        using var root = new CounterFlowContext();
        using var replacement = new CounterFlowContext();
        using var entry = new RefEntry<object>(null);
        using var child = new FlowContext(root).With(entry);

        Assert.Same(root.Input, child.GetRequired<ICounterInputAccess>());
        entry.Value = replacement.Input;

        Assert.Same(replacement.Input, child.GetRequired<ICounterInputAccess>());
        Assert.Same(root.Input, root.GetRequired<ICounterInputAccess>());
        Assert.NotSame(root.Input, child.GetRequired<ICounterInputAccess>());
    }

    [Fact]
    public void PropertiesRejectDisposedContextAndTypedChildRetainsSharedAccess()
    {
        using var root = new CounterFlowContext();
        using var child = new CounterFlowContext(root);
        var input = root.Input;
        var state = root.State;
        var display = root.Display;

        root.Dispose();

        Assert.Throws<ObjectDisposedException>(() => root.Input);
        Assert.Throws<ObjectDisposedException>(() => root.State);
        Assert.Throws<ObjectDisposedException>(() => root.Display);
        Assert.Throws<ObjectDisposedException>(() => root.CreateIndependentContext());
        Assert.Same(input, child.Input);
        Assert.Same(state, child.State);
        Assert.Same(display, child.Display);
        Assert.Same(input, child.GetRequired<ICounterInputAccess>());
        state.RequestExit();
        Assert.True(child.State.ExitRequested);
    }

    [Fact]
    public void TypedChildConstructorRejectsNullAndDisposedParent()
    {
        Assert.Throws<ArgumentNullException>(() => new CounterFlowContext((CounterFlowContext)null!));
        using var parent = new CounterFlowContext();
        parent.Dispose();

        Assert.Throws<ObjectDisposedException>(() => new CounterFlowContext(parent));
    }

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

    private sealed class CustomEntry(string value) : Entry<string>(value) { }

    private sealed class DerivedEntry() : RefEntry<object>(null) { }
}
