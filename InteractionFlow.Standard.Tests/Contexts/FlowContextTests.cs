using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace InteractionFlow.Standard.Tests.Contexts
{
    public sealed class FlowContextTests
    {
        [Fact]
        public void RootProvidesItsCancellationAndAbsenceDoesNotThrow()
        {
            using var context = new FlowContext();
            Assert.Same(context.Cancellation, context.GetRequired<CancellationObject>());
            Assert.False(context.TryGet<string>(out _));
        }

        [Fact]
        public void AddedValuesOverrideDerivedAndParentValuesInReverseOrder()
        {
            using var parent = new Probe();
            parent.With("parent").With(42);
            using var child = new FlowContext(parent).With("first").With("last");
            Assert.Equal("last", child.GetRequired<string>());
            Assert.Equal(42, child.GetRequired<int>());
            Assert.Equal("parent", parent.GetRequired<string>());
            Assert.Equal(0, parent.LookupCount);
        }

        [Fact]
        public void ChildSharesCancellationButPreservesParentLookupOverrides()
        {
            using var parent = new FlowContext();
            var overrideValue = new CancellationObject();
            parent.With(overrideValue);
            using var child = new FlowContext(parent);
            Assert.Same(parent.Cancellation, child.Cancellation);
            Assert.Same(overrideValue, child.GetRequired<CancellationObject>());
            Assert.NotSame(overrideValue, child.Cancellation);
        }

        [Fact]
        public void MissingEntryContinuesSearchingButCircularEntryPropagatesFailure()
        {
            using var context = new FlowContext().With("fallback").With(new RefEntry<string>(null));
            Assert.Equal("fallback", context.GetRequired<string>());
            var circular = new RefEntry<object>(null);
            circular.Value = circular;
            context.With(circular);
            var error = Assert.Throws<ResultException>(() => context.GetRequired<string>());
            Assert.IsType<InvalidOperationException>(error.InnerException);
        }

        [Fact]
        public void DisposedContextDoesNotCallTheDerivedLookupHook()
        {
            var context = new Probe();
            Assert.Equal("derived", context.GetRequired<string>());
            Assert.Equal(1, context.LookupCount);
            context.Dispose();
            Assert.Throws<ObjectDisposedException>(() => context.GetRequired<string>());
            Assert.Throws<ObjectDisposedException>(() => context.With("new"));
            Assert.Equal(1, context.LookupCount);
            Assert.Equal(1, context.DisposeCount);
        }

        [Fact]
        public void ThrowingDisposeHookStillLeavesTheContextDisposedAndRunsOnlyOnce()
        {
            var failure = new FormatException("Cleanup failed");
            var context = new Probe();
            context.DuringDispose = () =>
            {
                Assert.Throws<ObjectDisposedException>(() => context.With("new"));
                Assert.Throws<ObjectDisposedException>(() => context.GetRequired<string>());
                context.Dispose(); // Reentrant disposal must not run the hook again.
                throw failure;
            };
            Assert.Same(failure, Assert.Throws<FormatException>(context.Dispose));
            context.Dispose();
            Assert.Equal(1, context.DisposeCount);
            Assert.Throws<ObjectDisposedException>(() => context.TryGet<string>(out _));
        }

        [Fact]
        public void ChildDisposalDoesNotDisposeParentEntriesOrAddedValues()
        {
            using var parent = new FlowContext().With("parent");
            var borrowed = new Borrowed();
            var entry = new RefEntry<Borrowed>(borrowed);
            var child = new FlowContext(parent).With(entry);
            child.Dispose();
            Assert.Equal("parent", parent.GetRequired<string>());
            Assert.Same(borrowed, entry.Value);
            Assert.False(borrowed.Disposed);
            Assert.Same(parent.Cancellation, child.Cancellation);
            Assert.False(parent.Cancellation.IsCancellationRequested);
            parent.With(42);
            Assert.Equal(42, parent.GetRequired<int>());
        }

        [Fact]
        public void DisposedParentIsOnlyConsultedWhenLocalLookupMisses()
        {
            var parent = new FlowContext().With(42);
            using var child = new FlowContext(parent).With("local");
            parent.Dispose();
            Assert.Equal("local", child.GetRequired<string>());
            Assert.Throws<ObjectDisposedException>(() => child.GetRequired<int>());
            Assert.Same(parent.Cancellation, child.Cancellation);
        }

        [Fact]
        public void NullParentAndNullValueAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new FlowContext(null!));
            using var context = new FlowContext();
            Assert.Throws<ArgumentNullException>(() => context.With<string>(null!));
        }

        private sealed class Probe : FlowContext
        {
            public int LookupCount { get; private set; }
            public int DisposeCount { get; private set; }
            public Action? DuringDispose { get; set; }

            protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
            {
                LookupCount++;
                if ("derived" is T found) { value = found; return true; }
                return base.TryGetCore(out value);
            }

            protected override void DisposeCore()
            {
                DisposeCount++;
                DuringDispose?.Invoke();
            }
        }

        private sealed class Borrowed : IDisposable
        {
            public bool Disposed { get; private set; }
            public void Dispose() => Disposed = true;
        }
    }
}
