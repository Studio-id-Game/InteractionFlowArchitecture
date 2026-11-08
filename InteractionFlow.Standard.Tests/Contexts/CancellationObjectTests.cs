using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Standard.Tests.Contexts
{
    public sealed class CancellationObjectTests
    {
        private static TaskCompletionSource<bool> Work() => new(TaskCreationOptions.RunContinuationsAsynchronously);
        private static async Task<Result> Finish(ValueTask<Result> wait)
            => await wait.AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        [Fact]
        public void RegistrationOnlyTracksItsOwnLifetime()
        {
            var cancellation = new CancellationObject();
            var work = Work();
            using var registration = cancellation.AddCancelableTask(work.Task);
            Assert.True(cancellation.HasTask);
            registration.Dispose();
            registration.Dispose();
            Assert.False(cancellation.HasTask);
            Assert.False(cancellation.IsCancellationRequested);
            Assert.False(work.Task.IsCompleted);
            work.SetResult(true);
        }

        [Fact]
        public async Task DuplicateRegistrationsAreIndependentUnderConcurrentDispose()
        {
            var cancellation = new CancellationObject();
            var work = Work();
            using var retained = cancellation.AddCancelableTask(work.Task);
            using var released = cancellation.AddCancelableTask(work.Task);
            await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(released.Dispose,
                TestContext.Current.CancellationToken)));
            Assert.True(cancellation.HasTask);
            retained.Dispose();
            Assert.False(cancellation.HasTask);
            work.SetResult(true);
        }

        [Fact]
        public async Task WaitRetainsTheCancelledTokenAndFixedSnapshotUntilAllTasksFinish()
        {
            var cancellation = new CancellationObject();
            var token = cancellation.GetToken();
            var work = Work();
            using var registration = cancellation.AddCancelableTask(work.Task);
            cancellation.Cancel();
            var wait = cancellation.WaitForTasksAndResetCancellationAsync();
            Assert.False(wait.IsCompleted);
            Assert.True(cancellation.IsCancellationRequested);
            Assert.Equal(token, cancellation.GetToken());
            Assert.True(cancellation.TryGetCanceledException(out var cancelled));
            Assert.Equal(token, cancelled.CancellationToken);
            Assert.Throws<InvalidOperationException>(() => cancellation.AddCancelableTask(Task.CompletedTask));
            var duplicate = await cancellation.WaitForTasksAndResetCancellationAsync();
            Assert.False(duplicate.Try(out var duplicateError));
            Assert.IsType<InvalidOperationException>(duplicateError!.InnerException);
            registration.Dispose();
            Assert.True(cancellation.HasTask); // Snapshot still owns the wait, even after deregistration.
            work.SetResult(true);
            Assert.True((await Finish(wait)).Try(out _));
            Assert.False(cancellation.HasTask);
            Assert.False(cancellation.IsCancellationRequested);
            Assert.True(token.IsCancellationRequested);
            Assert.NotEqual(token, cancellation.GetToken());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FaultedCancellationExceptionIsReportedRegardlessOfCompletionTiming(bool delayed)
        {
            var cancellation = new CancellationObject();
            var work = Work();
            var failure = new OperationCanceledException("Faulted, not cancelled");
            using var registration = cancellation.AddCancelableTask(work.Task);
            if (!delayed) work.SetException(failure);
            cancellation.Cancel();
            var wait = cancellation.WaitForTasksAndResetCancellationAsync();
            if (delayed) work.SetException(failure);
            var result = await Finish(wait);
            Assert.False(result.Try(out var error));
            var aggregate = Assert.IsType<AggregateException>(error!.InnerException);
            Assert.Same(failure, Assert.Single(aggregate.InnerExceptions));
            Assert.False(cancellation.IsCancellationRequested);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CancelledTaskIsSuccessfulRegardlessOfCompletionTiming(bool delayed)
        {
            var cancellation = new CancellationObject();
            var work = Work();
            using var registration = cancellation.AddCancelableTask(work.Task);
            if (!delayed) work.SetCanceled(new CancellationToken(true));
            cancellation.Cancel();
            var wait = cancellation.WaitForTasksAndResetCancellationAsync();
            if (delayed) work.SetCanceled(new CancellationToken(true));
            Assert.True((await Finish(wait)).Try(out _));
        }

        [Fact]
        public async Task AllTasksAreWaitedAndAllFaultsAreRetainedBeforeReset()
        {
            var cancellation = new CancellationObject();
            var first = Work();
            var second = Work();
            var failure1 = new FormatException("First");
            var failure2 = new ArgumentException("Second");
            var failure3 = new InvalidOperationException("Third");
            using var registration1 = cancellation.AddCancelableTask(first.Task);
            using var registration2 = cancellation.AddCancelableTask(second.Task);
            cancellation.Cancel();
            var wait = cancellation.WaitForTasksAndResetCancellationAsync();
            second.SetException([failure2, failure3]);
            Assert.False(wait.IsCompleted);
            Assert.True(cancellation.IsCancellationRequested);
            first.SetException(failure1);
            var result = await Finish(wait);
            Assert.False(result.Try(out var error));
            var aggregate = Assert.IsType<AggregateException>(error!.InnerException);
            Assert.Equal(new Exception[] { failure1, failure2, failure3 }, aggregate.InnerExceptions);
            Assert.False(cancellation.IsCancellationRequested);
            using var next = cancellation.AddCancelableTask(Task.CompletedTask);
        }

        [Fact]
        public async Task FaultedTaskIsNotDiscardedWhenManyTasksAreRegistered()
        {
            var cancellation = new CancellationObject();
            var failure = new FormatException("Must not be compacted away");
            using var failed = cancellation.AddCancelableTask(Task.FromException(failure));
            var registrations = new List<IDisposable>();
            try
            {
                for (var i = 0; i < 96; i++) registrations.Add(cancellation.AddCancelableTask(Task.CompletedTask));
                cancellation.Cancel();
                var result = await Finish(cancellation.WaitForTasksAndResetCancellationAsync());
                Assert.False(result.Try(out var error));
                Assert.Same(failure, Assert.Single(Assert.IsType<AggregateException>(error!.InnerException).InnerExceptions));
            }
            finally { foreach (var registration in registrations) registration.Dispose(); }
        }

        [Fact]
        public async Task OldRegistrationCannotRemoveANewRegistrationOfTheSameTask()
        {
            var cancellation = new CancellationObject();
            var failure = new FormatException("Retained by each registration");
            var task = Task.FromException(failure);
            using var old = cancellation.AddCancelableTask(task);
            cancellation.Cancel();
            Assert.False((await Finish(cancellation.WaitForTasksAndResetCancellationAsync())).Try(out _));
            using var current = cancellation.AddCancelableTask(task);
            old.Dispose();
            cancellation.Cancel();
            Assert.False((await Finish(cancellation.WaitForTasksAndResetCancellationAsync())).Try(out _));
        }

        [Fact]
        public async Task ResetWithoutRequestDoesNotRemoveRegistrations()
        {
            var cancellation = new CancellationObject();
            var work = Work();
            using var registration = cancellation.AddCancelableTask(work.Task);
            Assert.False((await cancellation.WaitForTasksAndResetCancellationAsync()).Try(out _));
            Assert.True(cancellation.HasTask);
            registration.Dispose();
            work.SetResult(true);
        }

        [Fact]
        public void NullRegistrationIsRejectedWithoutChangingState()
        {
            var cancellation = new CancellationObject();
            Assert.Throws<ArgumentNullException>(() => cancellation.AddCancelableTask(null!));
            Assert.False(cancellation.HasTask);
            Assert.False(cancellation.IsCancellationRequested);
        }

        [Fact]
        public void ResetFromCallbackDefersSourceDisposalWithoutWaitingForItself()
        {
            var cancellation = new CancellationObject();
            var token = cancellation.GetToken();
            using var callback = token.Register(() =>
            {
                var reset = cancellation.WaitForTasksAndResetCancellationAsync();
                Assert.True(reset.IsCompletedSuccessfully);
                Assert.True(reset.Result.Try(out _));
                Assert.False(cancellation.IsCancellationRequested);
                Assert.NotNull(token.WaitHandle); // Old source is still alive while Cancel is notifying.
                Assert.False(cancellation.GetToken().IsCancellationRequested);
            });
            cancellation.Cancel();
            Assert.Throws<ObjectDisposedException>(() => { _ = token.WaitHandle; });
        }

        [Fact]
        public async Task ConcurrentResetDoesNotDisposeASourceWhoseNotificationIsStillRunning()
        {
            var cancellation = new CancellationObject();
            var token = cancellation.GetToken();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var release = new ManualResetEventSlim();
            using var callback = token.Register(() =>
            {
                entered.SetResult();
                if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Test notification was not released");
            });
            var notifying = Task.Run(cancellation.Cancel, TestContext.Current.CancellationToken);
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                Assert.True((await Finish(cancellation.WaitForTasksAndResetCancellationAsync())).Try(out _));
                Assert.NotNull(token.WaitHandle);
                Assert.False(cancellation.GetToken().IsCancellationRequested);
            }
            finally { release.Set(); }
            await notifying.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Throws<ObjectDisposedException>(() => { _ = token.WaitHandle; });
        }

        [Fact]
        public void CallbackFailureStillReleasesRetiredSourceAndPropagatesFromCancel()
        {
            var cancellation = new CancellationObject();
            var token = cancellation.GetToken();
            var failure = new FormatException("Notification failed");
            using var callback = token.Register(() =>
            {
                var reset = cancellation.WaitForTasksAndResetCancellationAsync().AsTask();
                Assert.True(reset.GetAwaiter().GetResult().Try(out _));
                throw failure;
            });
            var actual = Assert.Throws<AggregateException>(cancellation.Cancel);
            Assert.Same(failure, Assert.Single(actual.InnerExceptions));
            Assert.Throws<ObjectDisposedException>(() => { _ = token.WaitHandle; });
        }

        [Fact]
        public async Task LegacyResetMethodForwardsToTheNewImplementation()
        {
            var cancellation = new CancellationObject();
            cancellation.Cancel();
#pragma warning disable CS0618 // Compatibility path is intentionally tested.
            var result = await cancellation.WaitAndResetAsync();
#pragma warning restore CS0618
            Assert.True(result.Try(out _));
            Assert.False(cancellation.IsCancellationRequested);
        }
    }
}
