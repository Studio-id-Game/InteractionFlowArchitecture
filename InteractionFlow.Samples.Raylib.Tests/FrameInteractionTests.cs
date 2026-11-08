using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.ExternalPorts;
using InteractionFlow.Samples.Raylib.Externals;
using InteractionFlow.Samples.Raylib.Interactions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.Raylib.Tests;

public sealed class FrameInteractionTests
{
    [Fact]
    public async Task PolledCommandIsDeliveredBeforeDrawing()
    {
        var counter = new RecordingCounter();
        var window = new TestWindow(() => Assert.Equal(new[] { CounterCommand.Increment }, counter.Commands));
        var input = new TestInput(frame => frame.Command = CounterCommand.Increment);
        using var context = new FlowContext().With(new FrameState());

        var end = await CreateInteraction(input, window, counter).ExecuteAsync(context);

        Assert.False(end.HasException);
        Assert.Equal(1, window.DrawCount);
    }

    [Fact]
    public async Task FrameWithoutCommandDrawsWithoutDeliveringInput()
    {
        var counter = new RecordingCounter();
        var window = new TestWindow();
        var input = new TestInput(frame => frame.Command = null);
        using var context = new FlowContext().With(new FrameState { Command = CounterCommand.Exit });

        var end = await CreateInteraction(input, window, counter).ExecuteAsync(context);

        Assert.False(end.HasException);
        Assert.Empty(counter.Commands);
        Assert.Equal(1, window.DrawCount);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task CloseOrCancelTakesPriorityOverCommand(bool close, bool cancel)
    {
        var counter = new RecordingCounter();
        var window = new TestWindow();
        var input = new TestInput(frame =>
        {
            frame.Command = CounterCommand.Increment;
            frame.CloseRequested = close;
            frame.CancelRequested = cancel;
        });
        using var context = new FlowContext().With(new FrameState());

        var end = await CreateInteraction(input, window, counter).ExecuteAsync(context);

        Assert.False(end.HasException);
        Assert.Empty(counter.Commands);
        Assert.Equal(1, window.DrawCount);
    }

    [Fact]
    public async Task BusyFrameInputIsDroppedInsteadOfQueuedForNextRead()
    {
        var counter = new CounterOperation();
        var state = new CounterState();
        var window = new TestWindow();
        var input = new TestInput(frame => frame.Command = CounterCommand.Increment);
        using var context = new FlowContext()
            .With(new FrameState())
            .With(state);
        var interaction = CreateInteraction(input, window, counter);
        var firstRead = counter.ReadAsync(context).AsTask();

        Assert.False((await interaction.ExecuteAsync(context)).HasException);
        Assert.Equal(CounterCommand.Increment, await firstRead.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken));
        Assert.True(state.BeginIncrement());

        // 現在の入力待機がないフレームでのコマンドは次回へ持ち越しません。
        Assert.False((await interaction.ExecuteAsync(context)).HasException);
        Assert.True(state.CompleteIncrement());
        var nextRead = counter.ReadAsync(context).AsTask();
        Assert.False(nextRead.IsCompleted);

        Assert.True(counter.Submit(context, CounterCommand.Exit));
        Assert.Equal(CounterCommand.Exit, await nextRead.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken));
        Assert.Equal(2, window.DrawCount);
    }

    private static PresentFrame CreateInteraction(
        IWindowOperation input,
        IWindowReaction window,
        ICounterOperation counter)
        => new(
            input,
            window,
            counter,
            new CounterExceptionHandling(),
            new CounterCancellationHandling());

    private sealed class TestInput(Action<FrameState> poll) : Operation, IWindowOperation
    {
        public override void ForceResetMemoryState()
        {
        }

        public void Poll(IFlowContext context) => poll(context.GetRequired<FrameState>());
    }

    private sealed class RecordingCounter : Operation, ICounterOperation
    {
        public List<CounterCommand> Commands { get; } = [];

        public override void ForceResetMemoryState()
        {
        }

        public bool Submit(IFlowContext context, CounterCommand command)
        {
            Commands.Add(command);
            return true;
        }

        public ValueTask<CounterCommand> ReadAsync(IFlowContext context)
            => throw new NotSupportedException();
    }

    private sealed class TestWindow(Action? onDraw = null) : Reaction, IWindowReaction
    {
        public int DrawCount { get; private set; }

        public override void ForceResetMemoryState()
        {
        }

        public ReactionEnd Open(IFlowContext context) => GetEnd();

        public ReactionEnd Draw(IFlowContext context)
        {
            onDraw?.Invoke();
            DrawCount++;
            return GetEnd();
        }

        public ReactionEnd Close(IFlowContext context, Result result) => GetEnd(result);
    }
}
