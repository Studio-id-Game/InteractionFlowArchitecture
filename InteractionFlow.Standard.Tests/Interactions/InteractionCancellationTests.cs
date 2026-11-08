using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Core.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Standard.Tests.Interactions
{
    public sealed class InteractionCancellationTests
    {
        [Fact]
        public async Task HookFinishesBeforeTheCancellationPortStartsAndCompletionNeverWaitsOnItself()
        {
            var context = new FlowContext();
            var errors = new Errors();
            var cancellations = new Cancellations();
            var hookEntered = Signal();
            var release = Signal();
            var interaction = new Probe(errors, cancellations)
            {
                Body = CancelBody,
                Hook = async _ => { hookEntered.SetResult(); await release.Task; }
            };
            var execution = interaction.ExecuteAsync(context);
            await hookEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(context.Cancellation.HasTask);
            Assert.False(execution.IsCompleted);
            Assert.Equal(0, cancellations.BeforeCount);
            release.SetResult();
            var end = await Finish(execution);
            Assert.False(end.HasException);
            Assert.Equal(1, interaction.HookCount);
            Assert.Equal(1, cancellations.BeforeCount);
            Assert.Equal(1, cancellations.AfterCount);
            Assert.Equal(0, errors.Count);
            Assert.False(context.Cancellation.HasTask);
            Assert.False(context.Cancellation.IsCancellationRequested);
        }

        [Fact]
        public async Task CancellationDuringAnAwaitedBodyRunsTheHookOnce()
        {
            var context = new FlowContext();
            var entered = Signal();
            var cancellations = new Cancellations();
            var interaction = new Probe(new Errors(), cancellations)
            {
                Body = async current =>
                {
                    entered.SetResult();
                    await Task.Delay(System.Threading.Timeout.Infinite, current.Cancellation.GetToken());
                    return Ends.Create();
                }
            };
            var execution = interaction.ExecuteAsync(context);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(context.Cancellation.HasTask);
            context.Cancellation.Cancel();
            Assert.False((await Finish(execution)).HasException);
            Assert.Equal(1, interaction.HookCount);
            Assert.Equal(1, cancellations.AfterCount);
        }

        [Fact]
        public async Task PreCancelledExecutionSkipsTheBodyAndHook()
        {
            var context = new FlowContext();
            context.Cancellation.Cancel();
            var cancellations = new Cancellations();
            var interaction = new Probe(new Errors(), cancellations) { Body = _ => throw new Exception("Must not run") };
            var end = await Finish(interaction.ExecuteAsync(context));
            Assert.False(end.HasException);
            Assert.Equal(0, interaction.BodyCount);
            Assert.Equal(0, interaction.HookCount);
            Assert.Equal(1, cancellations.AfterCount);
            Assert.False(context.Cancellation.IsCancellationRequested);
        }

        [Fact]
        public async Task SynchronousBodyCancellationPreservesTheExistingNoHookBehavior()
        {
            var context = new FlowContext();
            var cancellations = new Cancellations();
            var interaction = new Probe(new Errors(), cancellations)
            {
                Body = current =>
                {
                    current.Cancellation.Cancel();
                    throw new OperationCanceledException(current.Cancellation.GetToken());
                }
            };
            var end = await Finish(interaction.ExecuteAsync(context));
            Assert.False(end.HasException);
            Assert.Equal(1, interaction.BodyCount);
            Assert.Equal(0, interaction.HookCount);
            Assert.Equal(1, cancellations.AfterCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CleanupFailureIsRetainedAlongWithTheOriginalCancellation(bool cancelledHook)
        {
            var context = new FlowContext();
            var errors = new Errors();
            var cancellations = new Cancellations();
            Exception cleanupFailure = cancelledHook
                ? new OperationCanceledException("Hook was cancelled") : new FormatException("Hook failed");
            var original = new OperationCanceledException("Original body cancellation");
            var interaction = new Probe(errors, cancellations)
            {
                Body = current =>
                {
                    current.Cancellation.Cancel();
                    return Task.FromException<ReactionEnd>(original);
                },
                Hook = async _ => { await Task.Yield(); throw cleanupFailure; }
            };
            var end = await Finish(interaction.ExecuteAsync(context));
            Assert.True(end.HasException);
            Assert.Same(original, cancellations.Original!.InnerException);
            Assert.False(cancellations.ResetResult.Try(out var resetError));
            Assert.Contains(cleanupFailure, Leaves(resetError!));
            Assert.Contains(original, Leaves(end.Exception!));
            Assert.Contains(cleanupFailure, Leaves(end.Exception!));
            Assert.Equal(1, interaction.HookCount);
            Assert.Equal(0, errors.Count);
            Assert.False(context.Cancellation.IsCancellationRequested);
        }

        [Fact]
        public async Task SynchronousHookFailureAlsoRemainsOnTheCancellationPath()
        {
            var cancellations = new Cancellations();
            var failure = new InvalidOperationException("Synchronous hook failure");
            var interaction = new Probe(new Errors(), cancellations)
            {
                Body = CancelBody,
                Hook = _ => throw failure
            };
            var end = await Finish(interaction.ExecuteAsync(new FlowContext()));
            Assert.Contains(failure, Leaves(end.Exception!));
            Assert.Equal(1, cancellations.AfterCount);
        }

        [Fact]
        public async Task OrdinaryBodyFailureDoesNotReappearInALaterCancellationWait()
        {
            var context = new FlowContext();
            var errors = new Errors();
            var cancellations = new Cancellations();
            var failure = new FormatException("Body failed");
            var interaction = new Probe(errors, cancellations)
            {
                Body = _ => Task.FromException<ReactionEnd>(failure)
            };
            var end = await Finish(interaction.ExecuteAsync(context));
            Assert.Same(failure, end.Exception);
            Assert.Equal(1, errors.Count);
            Assert.Equal(0, interaction.HookCount);
            Assert.Equal(0, cancellations.AfterCount);
            context.Cancellation.Cancel();
            var reset = await context.Cancellation.WaitAndResetAsync();
            Assert.True(reset.Try(out _));
        }

        [Fact]
        public async Task CancellationHandlingAlsoWaitsForOtherRegisteredWork()
        {
            var context = new FlowContext();
            var other = Signal();
            context.Cancellation.AddCancelableTask(other.Task);
            var cancellations = new Cancellations();
            var interaction = new Probe(new Errors(), cancellations) { Body = CancelBody };
            var execution = interaction.ExecuteAsync(context);
            Assert.False(execution.IsCompleted);
            Assert.Equal(1, interaction.HookCount);
            Assert.Equal(1, cancellations.BeforeCount);
            Assert.Equal(0, cancellations.AfterCount);
            other.SetResult();
            Assert.False((await Finish(execution)).HasException);
            Assert.Equal(1, cancellations.AfterCount);
        }

        [Fact]
        public async Task SuccessDoesNotRunTheHookOrHandlingPorts()
        {
            var context = new FlowContext();
            var errors = new Errors();
            var cancellations = new Cancellations();
            var interaction = new Probe(errors, cancellations);
            var end = await Finish(interaction.ExecuteAsync(context));
            Assert.False(end.HasException);
            Assert.Same(context, end.LastContext);
            Assert.Equal(0, interaction.HookCount);
            Assert.Equal(0, errors.Count);
            Assert.Equal(0, cancellations.BeforeCount);
            Assert.False(context.Cancellation.HasTask);
        }

        [Fact]
        public async Task ReturnedCancellationReactionDoesNotInvokeTheExceptionHook()
        {
            var cancellations = new Cancellations();
            var cancelledEnd = Ends.Create(new OperationCanceledException());
            var interaction = new Probe(new Errors(), cancellations)
            {
                Body = _ => Task.FromResult(cancelledEnd)
            };
            var end = await Finish(interaction.ExecuteAsync(new FlowContext()));
            Assert.Same(cancelledEnd, end.End);
            Assert.Equal(0, interaction.HookCount);
            Assert.Equal(0, cancellations.BeforeCount);
        }

        [Fact]
        public async Task ARequestWithoutBodyCancellationIsNotImplicitlyHandled()
        {
            var context = new FlowContext();
            var cancellations = new Cancellations();
            var interaction = new Probe(new Errors(), cancellations)
            {
                Body = current =>
                {
                    current.Cancellation.Cancel();
                    return Task.FromResult(Ends.Create());
                }
            };
            Assert.False((await Finish(interaction.ExecuteAsync(context))).HasException);
            Assert.True(context.Cancellation.IsCancellationRequested);
            Assert.Equal(0, interaction.HookCount);
            Assert.Equal(0, cancellations.BeforeCount);
            Assert.False((await Finish(interaction.ExecuteAsync(context))).HasException);
            Assert.Equal(1, interaction.BodyCount); // The second execution is cancelled before entry.
            Assert.Equal(1, cancellations.BeforeCount);
        }

        [Fact]
        public async Task TheSameContextCanContinueAfterSuccessfulCancellationHandling()
        {
            var context = new FlowContext();
            var interaction = new Probe(new Errors(), new Cancellations()) { Body = CancelBody };
            Assert.False((await Finish(interaction.ExecuteAsync(context))).HasException);
            interaction.Body = _ => Task.FromResult(Ends.Create());
            Assert.False((await Finish(interaction.ExecuteAsync(context))).HasException);
            Assert.Equal(2, interaction.BodyCount);
            Assert.Equal(1, interaction.HookCount);
            Assert.False(context.Cancellation.IsCancellationRequested);
        }

        [Fact]
        public async Task CancellationPortExceptionPropagatesWithoutCallingExceptionPort()
        {
            var errors = new Errors();
            var failure = new InvalidOperationException("Port failed");
            var cancellations = new Cancellations { Failure = failure };
            var interaction = new Probe(errors, cancellations) { Body = CancelBody };
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
                () => Finish(interaction.ExecuteAsync(new FlowContext())));
            Assert.Same(failure, thrown);
            Assert.Equal(0, errors.Count);
            Assert.Equal(1, interaction.HookCount);
        }

        private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
        private static async Task<FlowEndToken> Finish(Task<FlowEndToken> execution)
            => await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        private static Task<ReactionEnd> CancelBody(IFlowContext context)
        {
            context.Cancellation.Cancel();
            return Task.FromCanceled<ReactionEnd>(context.Cancellation.GetToken());
        }

        private sealed class Probe(IExceptionPort<Exception> errors, ICancellationPort cancellations)
            : Interaction(errors, cancellations)
        {
            public Func<IFlowContext, Task<ReactionEnd>> Body { get; set; } = _ => Task.FromResult(Ends.Create());
            public Func<IFlowContext, Task> Hook { get; set; } = _ => Task.CompletedTask;
            public int BodyCount { get; private set; }
            public int HookCount { get; private set; }
            protected override Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
            {
                BodyCount++;
                return Body(context);
            }
            protected override Task OnCancellation(IFlowContext context)
            {
                HookCount++;
                return Hook(context);
            }
        }

        private sealed class Errors : ExceptionHandling
        {
            public int Count { get; private set; }
            public override void ForceResetMemoryState() { }
            protected override ValueTask<ReactionEnd> HandleExceptionCoreAsync(IFlowContext context, Exception error)
            {
                Count++;
                return new(GetEnd(error));
            }
        }

        private sealed class Cancellations : CancellationHandling
        {
            public int BeforeCount { get; private set; }
            public int AfterCount { get; private set; }
            public OperationCanceledException? Original { get; private set; }
            public Result ResetResult { get; private set; }
            public Exception? Failure { get; set; }
            public override void ForceResetMemoryState() { }
            protected override ValueTask BeforeCancellationCoreAsync(IFlowContext context, OperationCanceledException error)
            {
                BeforeCount++;
                Original = error;
                if (Failure != null) throw Failure;
                return default;
            }
            protected override ValueTask<ReactionEnd> AfterCancellationCoreAsync(
                IFlowContext context, OperationCanceledException error, Result result)
            {
                AfterCount++;
                ResetResult = result;
                return new(GetEnd(result.Try(out var failure) ? null : new AggregateException(error, failure)));
            }
        }

        private sealed class Ends : Reaction
        {
            public override void ForceResetMemoryState() { }
            public static ReactionEnd Create(Exception? error = null) => GetEnd(error);
        }

        private static IEnumerable<Exception> Leaves(Exception error)
        {
            if (error is ResultException wrapper && wrapper.InnerException != null) return Leaves(wrapper.InnerException);
            if (error is AggregateException aggregate) return aggregate.InnerExceptions.SelectMany(Leaves);
            if (error is OperationCanceledException cancellation && cancellation.InnerException != null)
                return Leaves(cancellation.InnerException);
            return new[] { error };
        }
    }
}
