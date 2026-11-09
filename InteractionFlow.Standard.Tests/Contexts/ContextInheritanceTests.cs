using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace InteractionFlow.Standard.Tests.Contexts
{
    public sealed class ContextInheritanceTests
    {
        [Theory]
        [InlineData(ContextInheritance.None, false, false)]
        [InlineData(ContextInheritance.Values, true, false)]
        [InlineData(ContextInheritance.Cancellation, false, true)]
        [InlineData(ContextInheritance.All, true, true)]
        public void ModesIndependentlySelectParentLookupAndCancellation(
            ContextInheritance mode, bool values, bool cancellation)
        {
            using var parent = new FlowContext().With("parent");
            using var child = new FlowContext(parent, mode);

            Assert.Equal(values, child.TryGet<string>(out var found));
            Assert.Equal(values ? "parent" : null, found);
            Assert.Equal(cancellation, ReferenceEquals(parent.Cancellation, child.Cancellation));
            Assert.Same(values ? parent.Cancellation : child.Cancellation,
                child.GetRequired<CancellationObject>());
            parent.With(42);
            Assert.Equal(values, child.TryGet<int>(out _)); // Values is live lookup, not a copy.
            child.With("local");
            Assert.Equal("local", child.GetRequired<string>());
            Assert.Equal("parent", parent.GetRequired<string>());

            parent.Cancellation.Cancel();
            Assert.Equal(cancellation, child.Cancellation.IsCancellationRequested);
        }

        [Fact]
        public void ExistingConstructorRetainsAllInheritanceAndParentOverrides()
        {
            using var parent = new FlowContext();
            var replacement = new CancellationObject();
            parent.With(replacement);
            using var legacy = new FlowContext(parent);
            using var explicitAll = new FlowContext(parent, ContextInheritance.All);
            Assert.Same(parent.Cancellation, legacy.Cancellation);
            Assert.Same(legacy.Cancellation, explicitAll.Cancellation);
            Assert.Same(replacement, legacy.GetRequired<CancellationObject>());
            Assert.Same(replacement, explicitAll.GetRequired<CancellationObject>());
        }

        [Fact]
        public void ValuesOnlyPreservesParentOverridesWithoutChangingOwnCancellation()
        {
            using var parent = new FlowContext();
            var replacement = new CancellationObject();
            parent.With(replacement);
            using var child = new FlowContext(parent, ContextInheritance.Values);
            Assert.NotSame(parent.Cancellation, child.Cancellation);
            Assert.Same(replacement, child.GetRequired<CancellationObject>());
            Assert.NotSame(replacement, child.Cancellation);
        }

        [Fact]
        public void ValuesOnlyDoesNotFallBackWhenParentDoesNotExposeCancellationAsValue()
        {
            using var child = new FlowContext(new HiddenCancellationContext(), ContextInheritance.Values);
            Assert.NotNull(child.Cancellation);
            Assert.False(child.TryGet<CancellationObject>(out _));
            Assert.Throws<RequiredContextValueNotFoundException>(() => child.GetRequired<CancellationObject>());
        }

        [Theory]
        [InlineData(ContextInheritance.None)]
        [InlineData(ContextInheritance.Values)]
        [InlineData(ContextInheritance.Cancellation)]
        [InlineData(ContextInheritance.All)]
        public void NullParentIsRejectedForEveryMode(ContextInheritance mode)
        {
            var error = Assert.Throws<ArgumentNullException>(() => new FlowContext(null!, mode));
            Assert.Equal("parentContext", error.ParamName);
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(-1)]
        public void UnknownFlagsAreRejectedBeforeConsultingParent(int bits)
        {
            var parent = new UnavailableContext();
            var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FlowContext(parent, (ContextInheritance)bits));
            Assert.Equal("inheritance", error.ParamName);
            Assert.Equal((ContextInheritance)bits, error.ActualValue);
        }

        [Theory]
        [InlineData(ContextInheritance.None)]
        [InlineData(ContextInheritance.Values)]
        public void UnsharedCancellationDoesNotReadParentProperty(ContextInheritance mode)
        {
            using var child = new FlowContext(new UnavailableContext(), mode);
            Assert.False(child.Cancellation.IsCancellationRequested);
            if (mode == ContextInheritance.None)
                Assert.False(child.TryGet<string>(out _));
            else
                Assert.Throws<InvalidOperationException>(() => child.TryGet<string>(out _));
        }

        [Theory]
        [InlineData(ContextInheritance.None, false)]
        [InlineData(ContextInheritance.Values, true)]
        [InlineData(ContextInheritance.Cancellation, false)]
        [InlineData(ContextInheritance.All, true)]
        public void ParentDisposalOnlyAffectsInheritedLookup(ContextInheritance mode, bool values)
        {
            using var parent = new FlowContext().With("parent");
            using var child = new FlowContext(parent, mode);
            parent.Dispose();
            child.With(42);
            Assert.Equal(42, child.GetRequired<int>());
            if (values)
                Assert.Throws<ObjectDisposedException>(() => child.TryGet<string>(out _));
            else
                Assert.False(child.TryGet<string>(out _));
            Assert.False(child.Cancellation.IsCancellationRequested);
        }

        [Theory]
        [InlineData(ContextInheritance.None)]
        [InlineData(ContextInheritance.Values)]
        [InlineData(ContextInheritance.Cancellation)]
        [InlineData(ContextInheritance.All)]
        public void DisposalDoesNotOwnParentOrCancelEitherSide(ContextInheritance mode)
        {
            using var parent = new FlowContext().With("parent");
            var child = new FlowContext(parent, mode);
            var cancellation = child.Cancellation;
            child.Dispose();
            child.Dispose();
            Assert.Same(cancellation, child.Cancellation);
            Assert.False(cancellation.IsCancellationRequested);
            Assert.False(parent.Cancellation.IsCancellationRequested);
            Assert.Equal("parent", parent.GetRequired<string>());
            Assert.Throws<ObjectDisposedException>(() => child.With("value"));
            Assert.Throws<ObjectDisposedException>(() => child.TryGet<string>(out _));
        }

        [Fact]
        public void CancellationOnlySupportsFixedPropertiesWithoutParentValueEscape()
        {
            using var parent = new FlowContext().With("hidden").With(42);
            using var child = new FixedContext(parent);
            Assert.Same(parent.Cancellation, child.Cancellation);
            Assert.Equal(child.Label, child.GetRequired<string>());
            Assert.False(child.TryGet<int>(out _));
            child.With("override");
            Assert.Equal("override", child.GetRequired<string>());
        }

        private sealed class FixedContext(IFlowContext parent)
            : FlowContext(parent, ContextInheritance.Cancellation)
        {
            public string Label => "fixed";

            protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
            {
                if (typeof(T) == typeof(string))
                {
                    value = (T)(object)Label;
                    return true;
                }
                return base.TryGetCore(out value);
            }
        }

        private sealed class UnavailableContext : IFlowContext
        {
            public CancellationObject Cancellation => throw new InvalidOperationException("Unexpected cancellation access.");
            public bool TryGet<T>([MaybeNullWhen(false)] out T value)
                => throw new InvalidOperationException("Unexpected value lookup.");
        }

        private sealed class HiddenCancellationContext : IFlowContext
        {
            public CancellationObject Cancellation { get; } = new();
            public bool TryGet<T>([MaybeNullWhen(false)] out T value)
            {
                value = default;
                return false;
            }
        }
    }
}
