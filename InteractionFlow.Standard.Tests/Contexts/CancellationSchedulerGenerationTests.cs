using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Standard.Tests.Contexts
{
    public sealed class CancellationSchedulerGenerationTests
    {
        private static CancellationToken TestCancellation => TestContext.Current.CancellationToken;

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task OldDeadlineDoesNotCancelTheResetStateAndStillWaitsForWork(bool generic, bool failWork)
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var work = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
            var valueWork = new TaskCompletionSource<Result<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var execution = generic ? RunWithValueAsync() : scheduler.TimeoutAsync(TimeSpan.FromMilliseconds(30), work.Task);
            cancellation.Cancel();
            Assert.True((await cancellation.WaitForTasksAndResetCancellationAsync()).Try(out _));
            var nextToken = cancellation.GetToken();
            var nextNotifications = 0;
            using var registration = nextToken.Register(() => Interlocked.Increment(ref nextNotifications));

            // Leave the original task pending beyond its deadline, so the scheduler can attempt its old notification.
            await Task.Delay(200, TestCancellation);
            Assert.False(nextToken.IsCancellationRequested);
            Assert.Equal(0, nextNotifications);
            Assert.False(execution.IsCompleted);

            var failure = new FormatException("Old work failed");
            if (generic) valueWork.SetResult(failWork ? (Result<string>)failure : "completed");
            else work.SetResult(failWork ? (Result)failure : Result.Success);
            var result = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestCancellation);
            Assert.Equal(!failWork, result.Try(out var error));
            if (failWork) Assert.Same(failure, error!.InnerException);
            Assert.False(nextToken.IsCancellationRequested);

            // Ordinary Cancel must still target the current state.
            cancellation.Cancel();
            Assert.True(nextToken.IsCancellationRequested);
            Assert.Equal(1, nextNotifications);

            async Task<Result> RunWithValueAsync()
                => (await scheduler.TimeoutAsync(TimeSpan.FromMilliseconds(30), valueWork.Task)).WithoutValue;
        }

        [Fact]
        public async Task EachTimeoutCapturesTheCurrentStateInsteadOfTheSchedulerCreationState()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            cancellation.Cancel();
            Assert.True((await cancellation.WaitForTasksAndResetCancellationAsync()).Try(out _));

            for (var i = 0; i < 2; i++)
            {
                var token = cancellation.GetToken();
                var work = new TaskCompletionSource<Result<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
                using var registration = token.Register(() => work.SetResult("completed"));
                var result = await scheduler.TimeoutAsync(TimeSpan.Zero, work.Task)
                    .WaitAsync(TimeSpan.FromSeconds(5), TestCancellation);
                Assert.True(result.Try(out var value, out _));
                Assert.Equal("completed", value);
                Assert.True(token.IsCancellationRequested);
                Assert.True((await cancellation.WaitForTasksAndResetCancellationAsync()).Try(out _));
            }
        }

        [Fact]
        public async Task ResetOnAnotherThreadDoesNotDisposeTheNotifyingStateOrCancelTheNextState()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var oldToken = cancellation.GetToken();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var release = new ManualResetEventSlim();
            using var registration = oldToken.Register(() =>
            {
                entered.SetResult();
                if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Notification not released");
            });
            var work = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
            var execution = Task.Run(() => scheduler.TimeoutAsync(TimeSpan.Zero, work.Task), TestCancellation);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestCancellation);
            try
            {
                Assert.True((await cancellation.WaitForTasksAndResetCancellationAsync()).Try(out _));
                Assert.NotNull(oldToken.WaitHandle);
                Assert.False(cancellation.GetToken().IsCancellationRequested);
                work.SetResult(Result.Success);
            }
            finally { release.Set(); }
            Assert.True((await execution.WaitAsync(TimeSpan.FromSeconds(5), TestCancellation)).Try(out _));
            Assert.False(cancellation.IsCancellationRequested);
            Assert.Throws<ObjectDisposedException>(() => { _ = oldToken.WaitHandle; });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResetInsideNotificationKeepsTheNewStateSafeUntilOldNotificationFinishes(bool failCallback)
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var oldToken = cancellation.GetToken();
            var nextToken = default(CancellationToken);
            var failure = new FormatException("Old callback failed");
            using var registration = oldToken.Register(() =>
            {
                var reset = cancellation.WaitForTasksAndResetCancellationAsync().AsTask();
                Assert.True(reset.GetAwaiter().GetResult().Try(out _));
                nextToken = cancellation.GetToken();
                if (failCallback) throw failure;
            });
            var work = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
            var execution = scheduler.TimeoutAsync(TimeSpan.Zero, work.Task);
            Assert.True(oldToken.IsCancellationRequested);
            Assert.False(nextToken.IsCancellationRequested);
            Assert.False(execution.IsCompleted);
            work.SetResult(Result.Success);
            var result = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestCancellation);
            Assert.Equal(!failCallback, result.Try(out var error));
            if (failCallback)
            {
                var aggregate = Assert.IsType<AggregateException>(error!.InnerException);
                Assert.Same(failure, Assert.Single(aggregate.InnerExceptions));
            }
            Assert.False(nextToken.IsCancellationRequested);
            Assert.Throws<ObjectDisposedException>(() => { _ = oldToken.WaitHandle; });
        }
    }
}
