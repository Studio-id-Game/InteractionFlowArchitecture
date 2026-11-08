using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Standard.Builders;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InteractionFlow.Standard.Tests.Interactions
{
    public sealed class TypedInteractionTests
    {
        [Fact]
        public async Task TypedContextReachesBodyAndPortWithoutConversion()
        {
            using var context = new SpecializedContext();
            var reaction = new TypedReaction();
            var interaction = new TypedPortInteraction(reaction, new Errors(), new Cancellations());

            var end = await interaction.ExecuteAsync(context);

            Assert.False(end.HasException);
            Assert.Same(context, reaction.Context);
            Assert.Equal("specialized", reaction.Value);
            Assert.Equal(FlowLayerTypes.Interaction, ((IFlowNode)interaction).Layer);
            Assert.Equal(FunctionPortTypes.None, ((IFlowNode)interaction).FunctionTypes);
            Assert.Contains(reaction, interaction.Dependency.ToArray());
        }

        [Fact]
        public async Task ContravarianceNarrowsBroadRequirementWithoutWideningTypedRequirement()
        {
            using var context = new SpecializedContext();
            IInteraction legacy = new LegacyInteraction(new Errors(), new Cancellations());
            IInteraction<IFlowContext> broad = legacy;
            IInteraction<SpecializedContext> narrowed = broad;

            Assert.False((await legacy.ExecuteAsync(context)).HasException);
            Assert.False((await narrowed.ExecuteAsync(context)).HasException);

            var typed = new Probe(new Errors(), new Cancellations());
            IInteractionNode node = typed;
            Assert.Same(typed, node);
            Assert.False(typeof(IInteraction).IsAssignableFrom(typed.GetType()));
            Assert.False(typeof(IInteraction<IFlowContext>).IsAssignableFrom(typed.GetType()));
            Assert.Null(typed.GetType().GetMethod(nameof(typed.ExecuteAsync), [typeof(IFlowContext)]));
            Assert.NotNull(typed.GetType().GetMethod(nameof(typed.ExecuteAsync), [typeof(SpecializedContext)]));
        }

        [Fact]
        public async Task DirectImplementationsCanCreateEndsThroughTheirExistingAndTypedContracts()
        {
            using var context = new SpecializedContext();
            IInteraction legacy = new DirectLegacyInteraction();
            IInteraction explicitLegacy = new DirectExplicitLegacyInteraction();
            IInteraction<SpecializedContext> typed = new DirectTypedInteraction();

            Assert.False((await legacy.ExecuteAsync(context)).HasException);
            Assert.False((await explicitLegacy.ExecuteAsync(context)).HasException);
            IInteraction<IFlowContext> genericLegacy = explicitLegacy;
            Assert.False((await genericLegacy.ExecuteAsync(context)).HasException);
            IInteraction<SpecializedContext> narrowedLegacy = genericLegacy;
            Assert.False((await narrowedLegacy.ExecuteAsync(context)).HasException);
            Assert.False((await typed.ExecuteAsync(context)).HasException);
            Assert.Equal(FlowLayerTypes.Interaction, ((IFlowNode)typed).Layer);
        }

        [Fact]
        public async Task ExistingRegistrationSyntaxResolvesTypedInteractionInTypedSystemFlow()
        {
            var builder = new ScopeBuilder();
            builder.UseFunction<ITypedReaction, TypedReaction>()
                .UseFunction<IExceptionPort<Exception>, Errors>()
                .UseFunction<ICancellationPort, Cancellations>()
                .UseInteraction<TypedPortInteraction>();
            using var scope = builder.BuildScope();
            using var flow = scope.BuildSystemFlow<TypedFlow, SpecializedContext>();
            using var context = new SpecializedContext();

            var end = await flow.ExecuteAsync(context);

            Assert.False(end.HasException);
            var reaction = Assert.IsType<TypedReaction>(scope.GetRequiredService<ITypedReaction>());
            Assert.Same(context, reaction.Context);
            Assert.Same(scope.GetRequiredService<TypedPortInteraction>(), scope.GetRequiredService<TypedPortInteraction>());
        }

        [Fact]
        public async Task TypedBodyFailureUsesExceptionPortAndUnregistersExecution()
        {
            using var context = new SpecializedContext();
            var failure = new FormatException("body failed");
            var errors = new Errors();
            var interaction = new Probe(errors, new Cancellations())
            {
                Body = _ => Task.FromException<ReactionEnd>(failure)
            };

            var end = await interaction.ExecuteAsync(context);

            Assert.Same(failure, end.Exception);
            Assert.Same(context, errors.Context);
            Assert.Equal(0, interaction.HookCount);
            Assert.False(context.Cancellation.HasTask);
        }

        [Fact]
        public async Task TypedCancellationHookCompletesBeforePortResetsRegisteredExecution()
        {
            using var context = new SpecializedContext();
            var hookEntered = Signal();
            var releaseHook = Signal();
            var cancellations = new Cancellations();
            var hookFinished = false;
            var interaction = new Probe(new Errors(), cancellations)
            {
                Body = async current =>
                {
                    await Task.Delay(System.Threading.Timeout.Infinite, current.Cancellation.GetToken());
                    return Ends.Create();
                },
                Hook = async current =>
                {
                    Assert.Same(context, current);
                    hookEntered.SetResult(true);
                    await releaseHook.Task;
                    hookFinished = true;
                }
            };
            var execution = interaction.ExecuteAsync(context);
            context.Cancellation.Cancel();
            await hookEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(context.Cancellation.HasTask);
            Assert.False(execution.IsCompleted);
            Assert.Equal(0, cancellations.AfterCount);
            releaseHook.SetResult(true);

            var end = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            Assert.False(end.HasException);
            Assert.True(hookFinished);
            Assert.Equal(1, interaction.HookCount);
            Assert.Same(context, cancellations.Context);
            Assert.Equal(1, cancellations.AfterCount);
            Assert.False(context.Cancellation.HasTask);
            Assert.False(context.Cancellation.IsCancellationRequested);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task StartupCancellationSkipsHookForPreCanceledAndSynchronousBody(bool preCanceled)
        {
            using var context = new SpecializedContext();
            var cancellations = new Cancellations();
            var interaction = new Probe(new Errors(), cancellations)
            {
                Body = current =>
                {
                    current.Cancellation.Cancel();
                    throw new OperationCanceledException(current.Cancellation.GetToken());
                }
            };
            if (preCanceled)
                context.Cancellation.Cancel();

            var end = await interaction.ExecuteAsync(context);

            Assert.False(end.HasException);
            Assert.Equal(preCanceled ? 0 : 1, interaction.BodyCount);
            Assert.Equal(0, interaction.HookCount);
            Assert.Equal(1, cancellations.AfterCount);
            Assert.False(context.Cancellation.HasTask);
            Assert.False(context.Cancellation.IsCancellationRequested);
        }

        [Fact]
        public async Task TypedCleanupFailureIsCollectedBeforeResetAndPreserved()
        {
            using var context = new SpecializedContext();
            var cleanup = new InvalidOperationException("cleanup failed");
            var cancellations = new Cancellations();
            var interaction = new Probe(new Errors(), cancellations)
            {
                Body = current =>
                {
                    current.Cancellation.Cancel();
                    return Task.FromCanceled<ReactionEnd>(current.Cancellation.GetToken());
                },
                Hook = _ => Task.FromException(cleanup)
            };

            var end = await interaction.ExecuteAsync(context);

            Assert.True(end.HasException);
            Assert.False(cancellations.WaitResult.Try(out var failure));
            Assert.Contains(cleanup, Assert.IsType<AggregateException>(failure!.InnerException).InnerExceptions);
            Assert.Equal(1, interaction.HookCount);
            Assert.False(context.Cancellation.HasTask);
            Assert.False(context.Cancellation.IsCancellationRequested);
        }

        private static TaskCompletionSource<bool> Signal()
            => new(TaskCreationOptions.RunContinuationsAsynchronously);

        private sealed class SpecializedContext : FlowContext
        {
            public string Value => "specialized";
        }

        private interface ITypedReaction : IReactionPort
        {
            ReactionEnd Complete(SpecializedContext context);
        }

        private sealed class TypedReaction : Reaction, ITypedReaction
        {
            public TypedReaction() { }
            public SpecializedContext? Context { get; private set; }
            public string? Value { get; private set; }
            public override void ForceResetMemoryState() { }

            public ReactionEnd Complete(SpecializedContext context)
            {
                Context = context;
                Value = context.Value;
                return GetEnd();
            }
        }

        private sealed class TypedPortInteraction(
            ITypedReaction reaction,
            IExceptionPort<Exception> exceptionPort,
            ICancellationPort cancellationPort)
            : Interaction<SpecializedContext>(exceptionPort, cancellationPort, reaction)
        {
            protected override Task<ReactionEnd> ExecuteCoreAsync(SpecializedContext context)
                => Task.FromResult(reaction.Complete(context));
        }

        private sealed class TypedFlow(TypedPortInteraction interaction)
            : SystemFlow<SpecializedContext>(interaction)
        {
            protected override Task<InteractionEnd> ExecuteCoreAsync(SpecializedContext context)
                => interaction.ExecuteAsync(context);
        }

        private sealed class Probe(IExceptionPort<Exception> errors, ICancellationPort cancellations)
            : Interaction<SpecializedContext>(errors, cancellations)
        {
            public Func<SpecializedContext, Task<ReactionEnd>> Body { get; init; }
                = _ => Task.FromResult(Ends.Create());
            public Func<SpecializedContext, Task> Hook { get; init; } = _ => Task.CompletedTask;
            public int BodyCount { get; private set; }
            public int HookCount { get; private set; }

            protected override Task<ReactionEnd> ExecuteCoreAsync(SpecializedContext context)
            {
                BodyCount++;
                return Body(context);
            }

            protected override Task OnCancellation(SpecializedContext context)
            {
                HookCount++;
                return Hook(context);
            }
        }

        private sealed class LegacyInteraction(IExceptionPort<Exception> errors, ICancellationPort cancellations)
            : Interaction(errors, cancellations)
        {
            protected override Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
                => Task.FromResult(Ends.Create());
        }

        private sealed class DirectLegacyInteraction : IInteraction
        {
            public ReadOnlyMemory<IDependencyNode> Dependency => ReadOnlyMemory<IDependencyNode>.Empty;
            public Task<InteractionEnd> ExecuteAsync(IFlowContext context)
                => Task.FromResult(IInteraction.GetEnd(Ends.Create()));
        }

        private sealed class DirectTypedInteraction : IInteraction<SpecializedContext>
        {
            public ReadOnlyMemory<IDependencyNode> Dependency => ReadOnlyMemory<IDependencyNode>.Empty;
            public Task<InteractionEnd> ExecuteAsync(SpecializedContext context)
                => Task.FromResult(IInteraction<SpecializedContext>.GetEnd(Ends.Create()));
        }

        private sealed class DirectExplicitLegacyInteraction : IInteraction
        {
            public ReadOnlyMemory<IDependencyNode> Dependency => ReadOnlyMemory<IDependencyNode>.Empty;

            Task<InteractionEnd> IInteraction.ExecuteAsync(IFlowContext context)
                => Task.FromResult(IInteraction.GetEnd(Ends.Create()));
        }

        private sealed class Errors : ExceptionHandling
        {
            public Errors() { }
            public IFlowContext? Context { get; private set; }
            public override void ForceResetMemoryState() { }

            protected override ValueTask<ReactionEnd> HandleExceptionCoreAsync(IFlowContext context, Exception exception)
            {
                Context = context;
                return new(GetEnd(exception));
            }
        }

        private sealed class Cancellations : CancellationHandling
        {
            public Cancellations() { }
            public IFlowContext? Context { get; private set; }
            public int AfterCount { get; private set; }
            public Result WaitResult { get; private set; }
            public override void ForceResetMemoryState() { }

            protected override ValueTask<ReactionEnd> AfterCancellationCoreAsync(
                IFlowContext context, OperationCanceledException exception, Result waitAndResetResult)
            {
                Context = context;
                AfterCount++;
                WaitResult = waitAndResetResult;
                return new(GetEnd(waitAndResetResult));
            }
        }

        private sealed class Ends : Reaction
        {
            public override void ForceResetMemoryState() { }
            public static ReactionEnd Create() => GetEnd();
        }
    }
}
