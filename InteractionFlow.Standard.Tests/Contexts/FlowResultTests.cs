using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.SystemFlows;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Standard.Tests.Contexts
{
    public sealed class FlowResultTests
    {
        [Fact]
        public void ReactionCanCompleteWithASuccessfulResult()
        {
            var end = Ends.Create(Result.Success);
            Assert.True(end.Result.Try(out _));
            Assert.False(end.HasException);
            Assert.False(end.HasCanceled);
            Assert.Null(end.Exception);
        }

        [Fact]
        public void ExistingFailureResultIsPreservedWithoutRewrapping()
        {
            var failure = new FormatException("Failed");
            Result result = failure;
            var end = Ends.Create(result);
            Assert.False(result.Try(out var expected));
            Assert.False(end.Result.Try(out var actual));
            Assert.Same(expected, actual);
            Assert.Same(failure, end.Exception);
            Assert.True(end.HasException);
        }

        [Fact]
        public void CancellationResultRemainsDistinguishableFromAggregateFailure()
        {
            var cancellation = new OperationCanceledException();
            var end = Ends.Create((Result)cancellation);
            Assert.True(end.HasCanceled);
            Assert.Same(cancellation, end.Exception);
            var aggregate = Ends.Create((Result)new AggregateException(cancellation));
            Assert.False(aggregate.HasCanceled);
            Assert.True(aggregate.HasException);
        }

        [Fact]
        public void ResultExceptionWithoutInnerExceptionIsPreserved()
        {
            var result = default(Result<string>).WithoutValue;
            Assert.False(result.Try(out var failure));
            var end = Ends.Create(result);
            Assert.Same(failure, end.Exception);
            Assert.False(end.Result.Try(out var actual));
            Assert.Same(failure, actual);
        }

        [Fact]
        public void ResultsCanBeComposedBeforeReactionCompletion()
        {
            Result failure = new FormatException("Recoverable");
            var recovered = failure.ThenError(_ => Result.Success);
            Assert.True(Ends.Create(recovered).Result.Try(out _));
        }

        [Fact]
        public async Task NestedSystemFlowsForwardTheSameTokenAndResult()
        {
            Result failure = new FormatException("Failure");
            var reaction = Ends.Create(failure);
            var interaction = new Probe(reaction);
            var token = await interaction.ExecuteAsync(new FlowContext());
            var inner = new Forward(token);
            var outer = new Nested(inner);
            var context = new FlowContext();
            var actual = await outer.ExecuteAsync(context);

            Assert.Same(token, actual);
            Assert.Same(reaction, actual.End);
            Assert.False(failure.Try(out var expected));
            Assert.False(actual.Result.Try(out var error));
            Assert.Same(expected, error);
        }

        [Fact]
        public async Task SystemFlowExceptionsContinueToPropagate()
        {
            var failure = new FormatException("Flow failed");
            var flow = new FailingFlow(failure);
            Assert.Same(failure, await Assert.ThrowsAsync<FormatException>(() => flow.ExecuteAsync(new FlowContext())));
        }

        private sealed class Ends : Reaction
        {
            public override void ForceResetMemoryState() { }
            public static ReactionEnd Create(Result result) => GetEnd(result);
        }

        private sealed class Probe(ReactionEnd end)
            : Interaction(new Handling(), new CancellationHandlingStub())
        {
            protected override Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context) => Task.FromResult(end);
        }

        private sealed class Handling : ExceptionHandling
        {
            public override void ForceResetMemoryState() { }
            protected override ValueTask<ReactionEnd> HandleExceptionCoreAsync(IFlowContext context, Exception exception)
                => new(GetEnd(exception));
        }

        private sealed class CancellationHandlingStub : CancellationHandling
        {
            public override void ForceResetMemoryState() { }
            protected override ValueTask<ReactionEnd> AfterCancellationCoreAsync(
                IFlowContext context, OperationCanceledException exception, Result waitAndResetResult)
                => new(GetEnd(waitAndResetResult));
        }

        private sealed class Forward(InteractionEnd token) : SystemFlow
        {
            protected override Task<InteractionEnd> ExecuteCoreAsync(IFlowContext context) => Task.FromResult(token);
        }

        private sealed class Nested(Forward child) : SystemFlow(child)
        {
            protected override Task<InteractionEnd> ExecuteCoreAsync(IFlowContext context)
                => child.ExecuteAsync(context);
        }

        private sealed class FailingFlow(Exception failure) : SystemFlow
        {
            protected override Task<InteractionEnd> ExecuteCoreAsync(IFlowContext context) => throw failure;
        }
    }
}
