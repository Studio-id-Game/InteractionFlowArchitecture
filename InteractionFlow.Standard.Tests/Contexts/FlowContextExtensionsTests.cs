using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace InteractionFlow.Standard.Tests.Contexts
{
    public sealed class FlowContextExtensionsTests
    {
        [Fact]
        public void ReturnsTheOriginalReference()
        {
            var value = new object();
            using var context = new FlowContext().With(value);
            Assert.Same(value, context.GetRequired<object>());
        }

        [Fact]
        public void ReturnsDefaultValuedStructWhenPresent()
        {
            using var context = new FlowContext().With(0);
            Assert.Equal(0, context.GetRequired<int>());
        }

        [Fact]
        public void MissingValueReportsTheRequiredType()
        {
            var context = new FlowContext();
            var error = Assert.Throws<RequiredContextValueNotFoundException>(() => context.GetRequired<string>());
            Assert.Equal(typeof(string), error.RequiredType);
            Assert.Contains(typeof(string).FullName!, error.Message);
            Assert.False(context.TryGet<string>(out _));
        }

        [Fact]
        public void MissingValueCapturesTheActualCallSite()
        {
            var context = new FlowContext();
            var expectedLine = CurrentLine() + 1;
            var error = Assert.Throws<RequiredContextValueNotFoundException>(() => context.GetRequired<string>());

            Assert.Equal(nameof(MissingValueCapturesTheActualCallSite), error.MemberName);
            Assert.Equal(nameof(FlowContextExtensionsTests) + ".cs", Path.GetFileName(error.FilePath));
            Assert.Equal(expectedLine, error.LineNumber);
            Assert.Contains($"FlowContextExtensionsTests.cs:{expectedLine} ({error.MemberName})", error.Message);
            Assert.DoesNotContain(Path.GetDirectoryName(error.FilePath)!, error.Message);
        }

        [Fact]
        public void ExplicitCallSiteIsForwardedToTheException()
        {
            var context = new FlowContext();
            var error = Assert.Throws<RequiredContextValueNotFoundException>(() =>
                context.GetRequired<int>("Run", "Sample.cs", 42));
            Assert.Equal("Run", error.MemberName);
            Assert.Equal("Sample.cs", error.FilePath);
            Assert.Equal(42, error.LineNumber);
            Assert.Contains("Requested at Sample.cs:42 (Run).", error.Message);
        }

        [Fact]
        public void ExceptionWithoutCallSiteKeepsTheTypeOnlyMessage()
        {
            var error = new RequiredContextValueNotFoundException(typeof(string));
            Assert.Equal("", error.MemberName);
            Assert.Equal("", error.FilePath);
            Assert.Equal(0, error.LineNumber);
            Assert.DoesNotContain("Requested at", error.Message);
        }

        private static int CurrentLine([CallerLineNumber] int line = 0) => line;

        [Fact]
        public void ScopedOverrideAndParentFallbackArePreserved()
        {
            using var parent = new FlowContext().With("parent").With(42);
            using (var child = new FlowContext(parent).With("first").With("last"))
            {
                Assert.Equal("last", child.GetRequired<string>());
                Assert.Equal(42, child.GetRequired<int>());
                Assert.Same(parent.Cancellation, child.GetRequired<CancellationObject>());
            }
            Assert.Equal("parent", parent.GetRequired<string>());
        }

        [Fact]
        public void MutableEntryIsReturnedWithoutCopying()
        {
            var entry = new RefEntry<int>(1);
            using var context = new FlowContext().With(entry);
            context.GetRequired<RefEntry<int>>().Value = 2;
            Assert.Equal(2, entry.Value);
        }

        [Fact]
        public void ResolutionFailureIsNotConvertedToMissingValue()
        {
            var failure = new FormatException("Resolution failed");
            var context = new FailingContext(failure);
            Assert.Same(failure, Assert.Throws<FormatException>(() => context.GetRequired<string>()));
        }

        [Fact]
        public void DisposedScopeStillReportsDisposal()
        {
            var context = new FlowContext().With("value");
            context.Dispose();
            Assert.Throws<ObjectDisposedException>(() => context.GetRequired<string>());
        }

        [Fact]
        public void NullArgumentsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => FlowContextExtensions.GetRequired<string>(null!));
            Assert.Throws<ArgumentNullException>(() => new RequiredContextValueNotFoundException(null!));
        }

        private sealed class FailingContext(Exception failure) : FlowContext
        {
            protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value) => throw failure;
        }
    }
}
