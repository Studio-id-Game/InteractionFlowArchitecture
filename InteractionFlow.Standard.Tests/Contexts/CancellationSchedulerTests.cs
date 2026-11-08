using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.Entities.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Standard.Tests.Contexts
{
    public sealed class CancellationSchedulerTests
    {
        private static CancellationToken TestCancellation => TestContext.Current.CancellationToken;

        private static TaskCompletionSource<Result> Work()
            => new(TaskCreationOptions.RunContinuationsAsynchronously);

        [Fact]
        public async Task AlreadyCompletedTaskWinsEvenAtZeroTimeout()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var result = await scheduler.TimeoutAsync(TimeSpan.Zero, Task.FromResult(Result.Success));
            Assert.True(result.Try(out _));
            Assert.False(cancellation.IsCancellationRequested);
        }

        [Fact]
        public async Task CompletedFailureResultIsPreservedWithoutAdditionalWrapping()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            Result original = new FormatException("Invalid data");
            var result = await scheduler.TimeoutAsync(TimeSpan.Zero, Task.FromResult(original));
            original.Try(out var expected);
            result.Try(out var actual);
            Assert.Same(expected, actual);
            Assert.False(cancellation.IsCancellationRequested);
        }

        [Fact]
        public async Task TimeoutNotifiesButWaitsForWorkToFinishWithoutResettingOrRegisteringTasks()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var work = Work();
            var resultTask = scheduler.TimeoutAsync(TimeSpan.Zero, work.Task);
            Assert.True(cancellation.IsCancellationRequested);
            Assert.False(cancellation.HasTask);
            Assert.False(resultTask.IsCompleted);
            work.SetResult(Result.Success);
            Assert.True((await resultTask.WaitAsync(TimeSpan.FromSeconds(5), TestCancellation)).Try(out _));
            Assert.True(cancellation.IsCancellationRequested);
        }

        [Fact]
        public async Task FiniteDeadlineNotifiesAfterTheAsynchronousTimerExpires()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var work = Work();
            using var registration = cancellation.GetToken().Register(() => work.SetResult(Result.Success));
            var result = await scheduler.TimeoutAsync(TimeSpan.FromMilliseconds(10), work.Task)
                .WaitAsync(TimeSpan.FromSeconds(5), TestCancellation);
            Assert.True(result.Try(out _));
            Assert.True(cancellation.IsCancellationRequested);
            Assert.True(work.Task.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task TimeoutWaitsForWorkEvenWhenNotificationThrows()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var failure = new InvalidOperationException("Callback failed");
            using var registration = cancellation.GetToken().Register(() => throw failure);
            var work = Work();
            var resultTask = scheduler.TimeoutAsync(TimeSpan.Zero, work.Task);
            Assert.False(resultTask.IsCompleted);
            work.SetResult(Result.Success);
            var result = await resultTask.WaitAsync(TimeSpan.FromSeconds(5), TestCancellation);
            Assert.False(result.Try(out var error));
            Assert.Contains(failure, Leaves(error!));
        }

        [Fact]
        public async Task NotificationFailureAndReturnedFailureAreBothRetained()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var notificationFailure = new InvalidOperationException("Callback failed");
            var workFailure = new FormatException("Work failed");
            var work = Work();
            using var registration = cancellation.GetToken().Register(() =>
            {
                work.SetResult(workFailure);
                throw notificationFailure;
            });
            var result = await scheduler.TimeoutAsync(TimeSpan.Zero, work.Task)
                .WaitAsync(TimeSpan.FromSeconds(5), TestCancellation);
            Assert.False(result.Try(out var error));
            Assert.IsType<AggregateException>(error!.InnerException);
            Assert.Equal([notificationFailure, workFailure], Leaves(error));
        }

        [Fact]
        public async Task NotificationFailureAndEveryFaultedTaskExceptionAreRetained()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var notificationFailure = new InvalidOperationException("Callback failed");
            var firstFailure = new FormatException("First work failure");
            var secondFailure = new ArgumentException("Second work failure");
            var work = Work();
            using var registration = cancellation.GetToken().Register(() =>
            {
                work.SetException([firstFailure, secondFailure]);
                throw notificationFailure;
            });
            var result = await scheduler.TimeoutAsync(TimeSpan.Zero, work.Task)
                .WaitAsync(TimeSpan.FromSeconds(5), TestCancellation);
            Assert.False(result.Try(out var error));
            Assert.Equal([notificationFailure, firstFailure, secondFailure], Leaves(error!));
        }

        [Fact]
        public async Task FaultedTaskIsConvertedAtTheTimeoutBoundary()
        {
            using var scheduler = new CancellationObject().GetScheduler();
            var failure = new FormatException("Work threw");
            var result = await scheduler.TimeoutAsync(Timeout.InfiniteTimeSpan, Task.FromException<Result>(failure));
            Assert.False(result.Try(out var error));
            Assert.Same(failure, error!.InnerException);
        }

        [Fact]
        public async Task TaskCancellationIsReturnedAsFailure()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var work = Work();
            var token = cancellation.GetToken();
            using var registration = token.Register(() => work.SetCanceled(token));
            var result = await scheduler.TimeoutAsync(TimeSpan.Zero, work.Task)
                .WaitAsync(TimeSpan.FromSeconds(5), TestCancellation);
            Assert.False(result.Try(out var error));
            var exception = Assert.IsType<OperationCanceledException>(error!.InnerException, exactMatch: false);
            Assert.Equal(token, exception.CancellationToken);
        }

        [Fact]
        public async Task DisposeStopsTheDeadlineButStillWaitsForTheOriginalWork()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var work = Work();
            var resultTask = scheduler.TimeoutAsync(TimeSpan.FromHours(1), work.Task);
            scheduler.Dispose();
            scheduler.Dispose();
            Assert.False(cancellation.IsCancellationRequested);
            Assert.False(resultTask.IsCompleted);
            work.SetResult(Result.Success);
            Assert.True((await resultTask.WaitAsync(TimeSpan.FromSeconds(5), TestCancellation)).Try(out _));
            Assert.False(cancellation.IsCancellationRequested);
            Assert.Throws<ObjectDisposedException>(() => { _ = scheduler.TimeoutAsync(TimeSpan.Zero, work.Task); });
        }

        [Fact]
        public async Task InfiniteTimeoutDoesNotNotify()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var work = Work();
            var resultTask = scheduler.TimeoutAsync(Timeout.InfiniteTimeSpan, work.Task);
            Assert.False(cancellation.IsCancellationRequested);
            work.SetResult(Result.Success);
            Assert.True((await resultTask).Try(out _));
            Assert.False(cancellation.IsCancellationRequested);
        }

        [Fact]
        public async Task SchedulerRejectsOverlapAndCanBeReusedAfterCompletion()
        {
            using var scheduler = new CancellationObject().GetScheduler();
            var work = Work();
            var first = scheduler.TimeoutAsync(Timeout.InfiniteTimeSpan, work.Task);
            Assert.Throws<InvalidOperationException>(() => { _ = scheduler.TimeoutAsync(TimeSpan.Zero, work.Task); });
            work.SetResult(Result.Success);
            await first;
            Assert.True((await scheduler.TimeoutAsync(TimeSpan.Zero, work.Task)).Try(out _));
        }

        [Fact]
        public async Task DisposeFromTheNotificationCallbackDoesNotWaitForItself()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var work = Work();
            using var registration = cancellation.GetToken().Register(() =>
            {
                scheduler.Dispose();
                work.SetResult(Result.Success);
            });
            var execution = Task.Run(() => scheduler.TimeoutAsync(TimeSpan.Zero, work.Task), TestCancellation);
            Assert.True((await execution.WaitAsync(TimeSpan.FromSeconds(5), TestCancellation)).Try(out _));
        }

        [Fact]
        public async Task DisposeWaitsForAnAlreadyRunningNotificationButNotTheWork()
        {
            var cancellation = new CancellationObject();
            using var scheduler = cancellation.GetScheduler();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var disposing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var release = new ManualResetEventSlim();
            var work = Work();
            using var registration = cancellation.GetToken().Register(() =>
            {
                entered.SetResult();
                if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Test release was not signaled.");
            });
            var execution = Task.Run(() => scheduler.TimeoutAsync(TimeSpan.Zero, work.Task), TestCancellation);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestCancellation);
            var disposal = Task.Run(() =>
            {
                disposing.SetResult();
                scheduler.Dispose();
            }, TestCancellation);
            try
            {
                await disposing.Task.WaitAsync(TimeSpan.FromSeconds(5), TestCancellation);
                Assert.NotSame(disposal, await Task.WhenAny(disposal, Task.Delay(50, TestCancellation)));
            }
            finally { release.Set(); }
            await disposal.WaitAsync(TimeSpan.FromSeconds(5), TestCancellation);
            Assert.False(execution.IsCompleted);
            work.SetResult(Result.Success);
            Assert.True((await execution.WaitAsync(TimeSpan.FromSeconds(5), TestCancellation)).Try(out _));
        }

        [Fact]
        public async Task SeparateSchedulersHaveIndependentLifetimes()
        {
            var cancellation = new CancellationObject();
            using var first = cancellation.GetScheduler();
            using var second = cancellation.GetScheduler();
            first.Dispose();
            var work = Work();
            using var registration = cancellation.GetToken().Register(() => work.SetResult(Result.Success));
            Assert.True((await second.TimeoutAsync(TimeSpan.Zero, work.Task)).Try(out _));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(2147483648)]
        public void InvalidTimeoutThrowsSynchronously(double milliseconds)
        {
            using var scheduler = new CancellationObject().GetScheduler();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _ = scheduler.TimeoutAsync(TimeSpan.FromMilliseconds(milliseconds), Task.FromResult(Result.Success));
            });
        }

        [Fact]
        public void NullArgumentsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => ((CancellationObject)null!).GetScheduler());
            using var scheduler = new CancellationObject().GetScheduler();
            Assert.Throws<ArgumentNullException>(() => { _ = scheduler.TimeoutAsync(TimeSpan.Zero, null!); });
        }

        private static IEnumerable<Exception> Leaves(Exception error)
        {
            if (error is ResultException wrapper && wrapper.InnerException != null) return Leaves(wrapper.InnerException);
            if (error is AggregateException aggregate) return aggregate.InnerExceptions.SelectMany(Leaves);
            return [error];
        }
    }
}
