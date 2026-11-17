using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ResultAPI.Entities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Standard.Tests.Entities
{
    public sealed class ResultThenTests
    {
        [Fact]
        public async Task ConvertsSuccessAfterAnAsyncReceiver()
        {
            var result = await Task.FromResult(42.AsResult())
                .ThenAsync(value => (Text: value.ToString(), Original: value).AsResult());
            Assert.True(result.HasValue(out var tuple));
            Assert.Equal(("42", 42), tuple);
        }

        [Fact]
        public async Task AsyncReceiverPreservesFailureWithoutCallingCallback()
        {
            Result<int> source = new InvalidOperationException("failure");
            Assert.True(source.HasError(out var original));
            var result = await Task.FromResult(source)
                .ThenAsync(Result<string> (int _) => throw new Exception("must not run"));
            Assert.True(result.HasError(out var error));
            Assert.Same(original, error);
        }

        [Fact]
        public async Task AsyncReceiverPropagatesSourceAndCallbackExceptions()
        {
            var failure = new InvalidOperationException("failure");
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Task.FromException<Result<int>>(failure)
                    .ThenAsync(Result<string> (int _) => throw new Exception("must not run"))));
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Task.FromResult(1.AsResult()).ThenAsync(Result<string> (int _) => throw failure)));
        }

        [Fact]
        public async Task AsyncReceiverRejectsInvalidSuccessValues()
        {
            await Assert.ThrowsAsync<ResultException>(() =>
                Task.FromResult(1.AsResult()).ThenAsync(_ => ((string)null!).AsResult()));
            await Assert.ThrowsAsync<ResultException>(() =>
                Task.FromResult(1.AsResult()).ThenAsync(_ => new Exception("invalid value").AsResult()));
        }

        [Fact]
        public void ConvertsSuccessToAnotherType()
        {
            var result = 42.AsResult().Then(value => (Text: value.ToString(), Original: value, Flag: true).AsResult());
            Assert.True(result.HasValue(out var tuple));
            Assert.Equal(("42", 42, true), tuple);
        }

        [Fact]
        public void PreservesFailureWithoutCallingCallback()
        {
            Result<int> source = new InvalidOperationException("failure");
            Assert.True(source.HasError(out var original));
            var result = source.Then<int, string>(_ => throw new Exception("must not run"));
            Assert.True(result.HasError(out var error));
            Assert.Same(original, error);
        }

        [Fact]
        public void UninitializedResultThrowsBeforeCallingCallback()
        {
            Assert.Throws<ResultException>(() =>
                default(Result<int>).Then<int, string>(_ => throw new Exception("must not run")));
        }

        [Fact]
        public void CallbackExceptionsPropagate()
        {
            var failure = new InvalidOperationException("callback");
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(
                () => 1.AsResult().Then<int, string>(_ => throw failure)));
        }

        [Fact]
        public void RejectsInvalidSuccessValues()
        {
            Assert.Throws<ResultException>(() => 1.AsResult().Then(_ => ((string)null!).AsResult()));
            Assert.Throws<ResultException>(() => 1.AsResult().Then(_ => new Exception("invalid value").AsResult()));
        }
    }
}
