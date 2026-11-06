using InteractionFlow.Core.Entities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Standard.Tests.Entities
{
    public sealed class ResultMapTests
    {
        [Fact]
        public async Task MapsSuccessAsynchronously()
        {
            var result = await 42.AsResult().StartAsync().MapAsync(value => (Text: value.ToString(), Original: value));
            Assert.True(result.Try(out var tuple, out _));
            Assert.Equal(("42", 42), tuple);
        }

        [Fact]
        public async Task AsyncMapPreservesFailureWithoutCallingMapper()
        {
            Result<int> source = new InvalidOperationException("failure");
            source.Try(out _, out var original);
            var result = await source.StartAsync().MapAsync<int, string>(_ => throw new Exception("must not run"));
            Assert.False(result.Try(out _, out var error));
            Assert.Same(original, error);
        }

        [Fact]
        public async Task AsyncMapPropagatesSourceAndMapperExceptions()
        {
            var failure = new InvalidOperationException("failure");
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Task.FromException<Result<int>>(failure).MapAsync<int, string>(_ => throw new Exception("must not run"))));
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                1.AsResult().StartAsync().MapAsync<int, string>(_ => throw failure)));
        }

        [Fact]
        public async Task AsyncMapRejectsInvalidSuccessValues()
        {
            await Assert.ThrowsAsync<ResultException>(() =>
                1.AsResult().StartAsync().MapAsync(_ => (string)null!));
            await Assert.ThrowsAsync<ResultException>(() =>
                1.AsResult().StartAsync().MapAsync(_ => new Exception("invalid value")));
        }

        [Fact]
        public void MapsSuccessToArbitraryType()
        {
            var result = 42.AsResult().Map(value => (Text: value.ToString(), Original: value, Flag: true));
            Assert.True(result.Try(out var tuple, out _));
            Assert.Equal(("42", 42, true), tuple);
        }

        [Fact]
        public void PreservesFailureWithoutCallingMapper()
        {
            Result<int> source = new InvalidOperationException("failure");
            source.Try(out _, out var original);
            var result = source.Map<int, string>(_ => throw new Exception("must not run"));
            Assert.False(result.Try(out _, out var error));
            Assert.Same(original, error);
        }

        [Fact]
        public void DefaultResultSkipsMapper()
        {
            var result = default(Result<int>).Map<int, string>(_ => throw new Exception("must not run"));
            Assert.False(result.Try(out _, out _));
        }

        [Fact]
        public void MapperExceptionsPropagate()
        {
            var failure = new InvalidOperationException("mapper");
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(
                () => 1.AsResult().Map<int, string>(_ => throw failure)));
        }

        [Fact]
        public void RejectsInvalidSuccessValues()
        {
            Assert.Throws<ResultException>(() => 1.AsResult().Map(_ => (string)null!));
            Assert.Throws<ResultException>(() => 1.AsResult().Map(_ => new Exception("invalid value")));
        }
    }
}
