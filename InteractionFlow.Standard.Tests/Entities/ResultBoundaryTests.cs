using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.ResultAPI.Entities;
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
                failed.ThenError(Result<int> (ResultException _) => throw failure)));
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                1.AsResult().IfSuccess(_ => throw failure)));
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                failed.Resolve(_ => "success", _ => throw failure)));
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                Result.Success.Then(() => throw failure)));
        }

        [Fact]
        public void FailedResultsSkipSuccessCallbacksAndSuccessfulResultsSkipRecovery()
        {
            Result<int> failed = new InvalidOperationException("failure");
            Assert.True(failed.HasError(out var original));
            Assert.True(failed.Then<int, string>(_ => throw new Exception("must not run")).HasError(out var error));
            Assert.Same(original, error);
            Assert.True(failed.IfSuccess(_ => throw new Exception("must not run")).HasError(out error));
            Assert.Same(original, error);
            var succeeded = 1.AsResult().ThenError(Result<int> (ResultException _) => throw new Exception("must not run"));
            Assert.True(succeeded.HasValue(out var value));
            Assert.Equal(1, value);
        }

        [Fact]
        public async Task AsyncExtensionsDoNotCatchFaultsOrCancellation()
        {
            var failure = new InvalidOperationException("callback");
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                1.AsResult().ThenAsync<int, string>(_ => Task.FromException<Result<string>>(failure))));
            Result<int> failed = failure;
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Task.FromResult(failed).ThenErrorAsync(Result<int> (ResultException _) => throw failure)));
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Task.FromResult(failed).ResolveAsync<int, string>(_ => Task.FromResult("success"), _ => throw failure)));
            var canceled = Task.FromCanceled<Result<int>>(new CancellationToken(canceled: true));
            var chained = canceled.ThenAsync(value => value.ToString().AsResult());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => chained);
            Assert.True(chained.IsCanceled);
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
