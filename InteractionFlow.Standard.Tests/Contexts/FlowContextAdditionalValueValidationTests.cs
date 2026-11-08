using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using System;
using Xunit;

namespace InteractionFlow.Standard.Tests.Contexts;

public sealed class FlowContextAdditionalValueValidationTests
{
    [Fact]
    public void DefaultValidationPreservesEntryResolutionAndChildOverride()
    {
        var original = new SampleValue("original");
        var replacement = new SampleValue("replacement");
        using var parent = new FlowContext().With(original);
        using var entry = new RefEntry<SampleValue>(replacement);
        using var child = new FlowContext(parent);

        Assert.Same(child, child.With<object>(entry));
        Assert.Same(replacement, child.GetRequired<SampleValue>());
        Assert.Same(original, parent.GetRequired<SampleValue>());
        Assert.Same(parent.Cancellation, child.Cancellation);
    }

    [Fact]
    public void ValidatorReceivesDeclaredTypeAndActualValueBeforeRegistration()
    {
        using var context = new RecordingContext();
        var value = new SampleValue("new");
        context.Validate = (type, actual) =>
        {
            Assert.Equal(typeof(object), type);
            Assert.Same(value, actual);
            Assert.False(context.TryGet<SampleValue>(out _));
        };

        Assert.Same(context, context.With<object>(value));
        Assert.Equal(1, context.ValidationCalls);
        Assert.Same(value, context.GetRequired<SampleValue>());
    }

    [Fact]
    public void RejectedAdditionPropagatesSameExceptionAndPreservesExistingValues()
    {
        using var context = new RecordingContext();
        var original = new SampleValue("original");
        var rejected = new SampleValue("rejected");
        context.With(original).With(42);
        var failure = new InvalidOperationException("Value rejected.");
        context.Validate = (_, _) => throw failure;

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => context.With(rejected)));
        Assert.Same(original, context.GetRequired<SampleValue>());
        Assert.Equal(42, context.GetRequired<int>());
        Assert.Equal(3, context.ValidationCalls);
    }

    [Fact]
    public void NullIsRejectedBeforeValidation()
    {
        using var context = new RecordingContext();

        var failure = Assert.Throws<ArgumentNullException>(() => context.With<string>(null!));

        Assert.Equal("value", failure.ParamName);
        Assert.Equal(0, context.ValidationCalls);
        Assert.False(context.TryGet<string>(out _));
    }

    [Fact]
    public void DisposalIsCheckedBeforeNullAndValidation()
    {
        var context = new RecordingContext();
        context.Dispose();

        Assert.Throws<ObjectDisposedException>(() => context.With("value"));
        Assert.Throws<ObjectDisposedException>(() => context.With<string>(null!));
        Assert.Equal(0, context.ValidationCalls);
    }

    [Fact]
    public void ParentValidationDoesNotConstrainOrdinaryChild()
    {
        using var parent = new RestrictedContext();
        using var child = new FlowContext(parent);
        var value = new BlockedValue();

        Assert.Throws<InvalidOperationException>(() => parent.With(value));
        child.With(value);

        Assert.Same(value, child.GetRequired<BlockedValue>());
        Assert.False(parent.TryGet<BlockedValue>(out _));
        Assert.Same(parent.Cancellation, child.Cancellation);
    }

    [Fact]
    public void InheritedValidationRunsThroughNonVirtualBaseEntryPoint()
    {
        using FlowContext context = new InheritedContext();

        Assert.Throws<InvalidOperationException>(() => context.With(new BlockedValue()));
        context.With("allowed");

        Assert.Equal("allowed", context.GetRequired<string>());
        Assert.False(context.TryGet<BlockedValue>(out _));
    }

    [Fact]
    public void FurtherOverrideCanComposeBaseAndDerivedValidation()
    {
        using FlowContext context = new CombinedContext();

        Assert.Throws<InvalidOperationException>(() => context.With(new BlockedValue()));
        Assert.Throws<ArgumentException>(() => context.With(-1));
        context.With(1);

        Assert.Equal(1, context.GetRequired<int>());
        Assert.False(context.TryGet<BlockedValue>(out _));
    }

    private sealed record SampleValue(string Text);
    private sealed class BlockedValue { }

    private sealed class RecordingContext : FlowContext
    {
        public int ValidationCalls { get; private set; }
        public Action<Type, object?>? Validate { get; set; }

        protected override void ValidateAdditionalValue<T>(T value)
        {
            ValidationCalls++;
            Validate?.Invoke(typeof(T), value);
        }
    }

    private class RestrictedContext : FlowContext
    {
        protected override void ValidateAdditionalValue<T>(T value)
        {
            if (value is BlockedValue)
                throw new InvalidOperationException("Blocked value.");
        }
    }

    private sealed class InheritedContext : RestrictedContext { }

    private sealed class CombinedContext : RestrictedContext
    {
        protected override void ValidateAdditionalValue<T>(T value)
        {
            base.ValidateAdditionalValue(value);
            if (value is int number && number < 0)
                throw new ArgumentException("Negative values are not allowed.", nameof(value));
        }
    }
}
