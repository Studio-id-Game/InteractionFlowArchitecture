using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ResultAPI.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ResultException = InteractionFlow.Core.Entities.ResultException;

namespace InteractionFlow.Standard.Tests.Contexts
{
    public sealed class CancellationSchedulerGenericTests
    {
        private static TaskCompletionSource<Result<string>> Work()
            => new(TaskCreationOptions.RunContinuationsAsynchronously);
        private static async Task<Result<string>> Finish(Task<Result<string>> task)
            => await task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        [Fact]
        public async Task CompletedValueWinsAtZeroTimeout()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var expected = new object();
            var result = await scheduler.TimeoutAsync(TimeSpan.Zero, Task.FromResult(expected.AsResult()));
            Assert.True(result.HasValue(out var value));
            Assert.Same(expected, value);
            Assert.False(cancellation.IsCancellationRequested);
        }

        [Fact]
        public async Task SuccessfulValueIsNotInterpretedAsAnotherResult()
        {
            using var scheduler = new CancellationObject().GetScheduler();
            Result innerFailure = new FormatException("Value contains a failure");
            var result = await scheduler.TimeoutAsync(TimeSpan.Zero, Task.FromResult(innerFailure.AsResult()));
            Assert.True(result.HasValue(out var inner));
            Assert.True(innerFailure.HasError(out var expected));
            Assert.True(inner.HasError(out var actual));
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task ReturnedFailureIsNotRewrapped()
        {
            using var scheduler = new CancellationObject().GetScheduler();
            Result<string> original = new FormatException("Work failed");
            var result = await scheduler.TimeoutAsync(TimeSpan.Zero, Task.FromResult(original));
            Assert.True(original.HasError(out var expected));
            Assert.True(result.HasError(out var actual));
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task UninitializedResultIsReturnedAsAnExplicitFailure()
        {
            using var scheduler = new CancellationObject().GetScheduler();
            var result = await scheduler.TimeoutAsync(TimeSpan.Zero, Task.FromResult(default(Result<string>)));

            Assert.True(result.HasError(out var error));
            Assert.Contains("is not initialized", error.Message);
        }

        [Fact]
        public async Task DeadlineWaitsForTheValueWithoutResettingOrRegisteringWork()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var work = Work();
            var task = scheduler.TimeoutAsync(TimeSpan.Zero, work.Task);
            Assert.True(cancellation.IsCancellationRequested);
            Assert.False(cancellation.HasTask);
            Assert.False(task.IsCompleted);
            work.SetResult("completed");
            Assert.True((await Finish(task)).HasValue(out var value));
            Assert.Equal("completed", value);
            Assert.True(cancellation.IsCancellationRequested);
        }

        [Fact]
        public async Task TimerCompletionReturnsTheValue()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var work = Work();
            using var registration = cancellation.GetToken().Register(() => work.SetResult("completed"));
            var result = await Finish(scheduler.TimeoutAsync(TimeSpan.FromMilliseconds(10), work.Task));
            Assert.True(result.HasValue(out var value));
            Assert.Equal("completed", value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task NotificationFailureWaitsForWorkAndCombinesReturnedFailures(bool failWork)
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var notification = new IOException("Notification failed");
            var workFailure = new FormatException("Work failed");
            using var registration = cancellation.GetToken().Register(() => throw notification);
            var work = Work();
            var task = scheduler.TimeoutAsync(TimeSpan.Zero, work.Task);
            Assert.False(task.IsCompleted);
            work.SetResult(failWork ? (Result<string>)workFailure : "completed");
            var result = await Finish(task);
            Assert.True(result.HasError(out var error));
            Assert.Equal(failWork ? new Exception[] { notification, workFailure } : [notification], Leaves(error));
        }

        [Fact]
        public async Task AllFaultedTaskExceptionsAreKeptAlongsideNotificationFailure()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var first = new FormatException("First");
            var second = new InvalidOperationException("Second");
            var notification = new IOException("Notification failed");
            var work = Work();
            using var registration = cancellation.GetToken().Register(() =>
            {
                work.SetException([first, second]);
                throw notification;
            });
            var result = await Finish(scheduler.TimeoutAsync(TimeSpan.Zero, work.Task));
            Assert.True(result.HasError(out var error));
            Assert.Equal([notification, first, second], Leaves(error));
        }

        [Fact]
        public async Task TaskCancellationIsConvertedToFailure()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var work = Work();
            var token = cancellation.GetToken();
            using var registration = token.Register(() => work.SetCanceled(token));
            var result = await Finish(scheduler.TimeoutAsync(TimeSpan.Zero, work.Task));
            Assert.True(result.HasError(out var error));
            var cancelled = Assert.IsType<OperationCanceledException>(error.InnerException, exactMatch: false);
            Assert.Equal(token, cancelled.CancellationToken);
        }

        [Fact]
        public async Task ThrownTaskFailureIsConvertedToFailure()
        {
            using var scheduler = new CancellationObject().GetScheduler();
            var failure = new FormatException("Task failed");
            var result = await scheduler.TimeoutAsync(Timeout.InfiniteTimeSpan,
                Task.FromException<Result<string>>(failure));
            Assert.True(result.HasError(out var error));
            Assert.Same(failure, error.InnerException);
        }

        [Fact]
        public async Task OverloadsShareTheActiveOperationAndCanBeReusedSequentially()
        {
            using var scheduler = new CancellationObject().GetScheduler();
            var work = Work();
            var first = scheduler.TimeoutAsync(Timeout.InfiniteTimeSpan, work.Task);
            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = scheduler.TimeoutAsync(TimeSpan.Zero, Task.FromResult(Result.Success));
            });
            work.SetResult("first");
            Assert.True((await Finish(first)).HasValue(out _));
            Assert.True((await scheduler.TimeoutAsync(TimeSpan.Zero, Task.FromResult(Result.Success))).IsSuccess);
            Assert.True((await scheduler.TimeoutAsync(TimeSpan.Zero, Task.FromResult("next".AsResult())))
                .HasValue(out var value));
            Assert.Equal("next", value);
        }

        [Fact]
        public async Task DisposeStopsTheTimerButStillWaitsForTheValue()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var work = Work();
            var task = scheduler.TimeoutAsync(TimeSpan.FromHours(1), work.Task);
            scheduler.Dispose();
            Assert.False(task.IsCompleted);
            Assert.False(cancellation.IsCancellationRequested);
            work.SetResult("completed");
            Assert.True((await Finish(task)).HasValue(out var value));
            Assert.Equal("completed", value);
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _ = scheduler.TimeoutAsync(TimeSpan.Zero, Task.FromResult("next".AsResult()));
            });
        }

        [Fact]
        public async Task DisposeFromNotificationDoesNotWaitForItself()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var work = Work();
            using var registration = cancellation.GetToken().Register(() =>
            {
                scheduler.Dispose();
                work.SetResult("completed");
            });
            var result = await Finish(scheduler.TimeoutAsync(TimeSpan.Zero, work.Task));
            Assert.True(result.HasValue(out var value));
            Assert.Equal("completed", value);
        }

        [Fact]
        public void InvalidArgumentsThrowSynchronously()
        {
            using var scheduler = new CancellationObject().GetScheduler();
            Assert.Throws<ArgumentNullException>(() => { _ = scheduler.TimeoutAsync<string>(TimeSpan.Zero, null!); });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _ = scheduler.TimeoutAsync(TimeSpan.FromMilliseconds(-2), Task.FromResult("value".AsResult()));
            });
        }

        private static IEnumerable<Exception> Leaves(Exception error)
        {
            if (error is ResultException wrapper && wrapper.InnerException != null) return Leaves(wrapper.InnerException);
            if (error is AggregateException aggregate) return aggregate.InnerExceptions.SelectMany(Leaves);
            return [error];
        }

    }
}
