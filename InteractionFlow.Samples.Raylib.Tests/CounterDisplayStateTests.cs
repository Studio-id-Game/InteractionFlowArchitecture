using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Raylib.Entities;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Samples.Raylib.Tests;

public sealed class CounterDisplayStateTests
{
    [Fact]
    public void ContextsShareDisplayStateAndSnapshotsRemainUnchangedAfterUpdates()
    {
        var display = new CounterDisplayState();
        using var root = new FlowContext().With(display);
        using var child = new FlowContext(root);

        Assert.Same(display, root.GetRequired<CounterDisplayState>());
        Assert.Same(display, child.GetRequired<CounterDisplayState>());

        display.Snapshot(out var initialCount, out var initialMessage, out var initiallyAcceptingInput);

        child.GetRequired<CounterDisplayState>().Show(3, "Completed");
        display.SetAcceptingInput(true);

        Assert.Equal(0, initialCount);
        Assert.Equal("Ready", initialMessage);
        Assert.False(initiallyAcceptingInput);

        root.GetRequired<CounterDisplayState>().Snapshot(out var count, out var message, out var acceptingInput);
        Assert.Equal(3, count);
        Assert.Equal("Completed", message);
        Assert.True(acceptingInput);
        Assert.Same(root.GetRequired<CounterDisplayState>(), child.GetRequired<CounterDisplayState>());
    }

    [Fact]
    public async Task ConcurrentAcceptingInputUpdatesDoNotRestoreStaleCountAndMessage()
    {
        var token = TestContext.Current.CancellationToken;

        await Task.WhenAll(Enumerable.Range(1, 256).Select(async count =>
        {
            var display = new CounterDisplayState();
            var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var show = Task.Run(async () =>
            {
                await start.Task;
                display.Show(count, "Completed " + count);
            }, token);

            var accept = Task.Run(async () =>
            {
                await start.Task;
                display.SetAcceptingInput(true);
            }, token);

            start.SetResult(true);
            await Task.WhenAll(show, accept);

            display.Snapshot(out var actualCount, out var message, out _);

            Assert.Equal(count, actualCount);
            Assert.Equal("Completed " + count, message);
            // Show が入力受付を閉じる仕様なので、最終 bool は実行順に従います。
        }));
    }
}
