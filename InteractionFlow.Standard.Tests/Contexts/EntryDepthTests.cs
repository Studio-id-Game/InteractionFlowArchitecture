using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using System;
using Xunit;

namespace InteractionFlow.Standard.Tests.Contexts;

public sealed class EntryDepthTests
{
    private static RefEntry<object> Chain(int count)
    {
        object value = "found";
        for (var i = 0; i < count; i++) value = new RefEntry<object>(value);
        return (RefEntry<object>)value;
    }

    [Theory]
    [InlineData(99)]
    [InlineData(100)]
    public void LimitCountsUserEntriesEquallyForParseAndContext(int depth)
    {
        var entry = Chain(depth);
        Assert.True(entry.Parse<string>().Try(out var found, out _));
        Assert.Equal("found", found);
        using var context = new FlowContext().With(entry);
        Assert.Equal("found", context.GetRequired<string>());
    }

    [Fact]
    public void AcyclicOverLimitFailsWithoutFallingBack()
    {
        var entry = Chain(101);
        Assert.False(entry.Parse<string>().Try(out _, out var failure));
        var error = Assert.IsType<EntryResolutionLimitException>(failure.InnerException);
        Assert.Equal(typeof(string), error.RequestedType);
        Assert.Equal(100, error.DepthLimit);
        Assert.Equal(100, error.DiagnosticDepth);
        Assert.False(error.CycleDetected);
        using var context = new FlowContext().With("fallback").With(entry);
        Assert.IsType<EntryResolutionLimitException>(
            Assert.Throws<ResultException>(() => context.TryGet<string>(out _)).InnerException);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void DiagnosisOnlyReportsCyclesWithinItsBound(int length, bool detected)
    {
        var root = new RefEntry<object>(null);
        var tail = root;
        for (var i = 1; i < length; i++)
        {
            var next = new RefEntry<object>(null);
            tail.Value = next;
            tail = next;
        }
        tail.Value = root;
        Assert.False(root.Parse<string>().Try(out _, out var failure));
        var error = Assert.IsType<EntryResolutionLimitException>(failure.InnerException);
        Assert.Equal(detected, error.CycleDetected);
        Assert.InRange(error.DiagnosticDepth, 1, 100);
    }

    [Fact]
    public void NormalNestedLookupDoesNotAllocate()
    {
        using var context = new FlowContext().With(Chain(100));
        for (var i = 0; i < 1000; i++) context.TryGet<string>(out _);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var success = true;
        for (var i = 0; i < 1000; i++) success &= context.TryGet<string>(out _);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(success);
        Assert.Equal(0L, bytes);
    }
}
