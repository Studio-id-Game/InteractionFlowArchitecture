using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.ExternalPorts;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Standard.Tests.Entities
{
    public sealed class ResultBoundaryTests
    {
        [Fact]
        public void ExtensionsDoNotCatchCallbackExceptions()
        {
            var failure = new InvalidOperationException("callback");
            Result<int> failed = failure;
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                1.AsResult().Then<int, string>(_ => throw failure)));
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                failed.ThenError(_ => throw failure)));
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                1.AsResult().OnSuccess(_ => throw failure)));
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                failed.Resolve(_ => "success", _ => throw failure)));
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                Result.Success.Then(() => throw failure)));
        }

        [Fact]
        public void FailedResultsSkipSuccessCallbacksAndSuccessfulResultsSkipRecovery()
        {
            Result<int> failed = new InvalidOperationException("failure");
            failed.Try(out _, out var original);
            Assert.False(failed.Then<int, string>(_ => throw new Exception("must not run")).Try(out _, out var error));
            Assert.Same(original, error);
            Assert.False(failed.OnSuccess(_ => throw new Exception("must not run")).Try(out _, out error));
            Assert.Same(original, error);
            Assert.True(1.AsResult().ThenError(_ => throw new Exception("must not run")).Try(out var value, out _));
            Assert.Equal(1, value);
        }

        [Fact]
        public async Task AsyncExtensionsDoNotCatchFaultsOrCancellation()
        {
            var failure = new InvalidOperationException("callback");
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                1.AsResult().StartAsync().ThenAsync<int, string>(_ => Task.FromException<Result<string>>(failure))));
            Result<int> failed = failure;
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                failed.StartAsync().ThenErrorAsync(_ => throw failure)));
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                failed.StartAsync().ResolveAsync(_ => Task.FromResult("success"), _ => throw failure)));
            var canceled = Task.FromCanceled<Result<int>>(new CancellationToken(canceled: true));
            var mapped = canceled.MapAsync(value => value.ToString());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mapped);
            Assert.True(mapped.IsCanceled);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task LoadAllPropagatesPortExceptions(bool asynchronous)
        {
            var failure = new InvalidOperationException("port");
            var port = new BatchPort(failure, asynchronous);
            var entries = new[] { new PersistentEntry<int, string>(1, "previous") };
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => entries.LoadAll(port)));
            Assert.Equal("previous", entries[0].Value);
        }

        private sealed class BatchPort(Exception failure, bool asynchronous) : IPersistencePort<int[], string?[]>
        {
            public ReadOnlyMemory<IDependencyNode> Dependency => ReadOnlyMemory<IDependencyNode>.Empty;
            public Task<Result<string?[]>> Load(int[] id, Result<string?[]> oldValue)
            {
                if (asynchronous) return Task.FromException<Result<string?[]>>(failure);
                throw failure;
            }
            public Task<Result> Save(int[] id, Result<string?[]> value) => throw new NotSupportedException();
            public Task<Result> Delete(int[] id) => throw new NotSupportedException();
            public Task<Result> Exists(int[] id) => throw new NotSupportedException();
            public Task<Result<int[][]>> GetAllIds() => throw new NotSupportedException();
        }
    }
}
