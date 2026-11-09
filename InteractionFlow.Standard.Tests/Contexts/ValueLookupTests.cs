using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using System;
using Xunit;

namespace InteractionFlow.Standard.Tests.Contexts;

public sealed class ValueLookupTests
{
    [Fact]
    public void OrdinaryLookupAndMissDoNotAllocateAfterWarmup()
    {
        using var parent = new FlowContext().With("parent");
        using var child = new FlowContext(parent).With(new object());

        bool Probe() => child.TryGet<string>(out var text) && text == "parent"
            && !child.TryGet<Uri>(out _);

        for (var i = 0; i < 1000; i++) Probe();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var success = true;
        for (var i = 0; i < 1000; i++) success &= Probe();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(success);
        Assert.Equal(0L, allocated);
    }

    [Fact]
    public void NestedMutableEntriesRemainLiveAndMissingValuesFallBack()
    {
        var leaf = new RefEntry<object>("first");
        var outer = new RefEntry<object>(leaf);
        using var context = new FlowContext().With("fallback").With(outer);

        Assert.Equal("first", context.GetRequired<string>());
        leaf.Value = "second";
        Assert.Equal("second", context.GetRequired<string>());
        leaf.Value = null;
        Assert.Equal("fallback", context.GetRequired<string>());
        leaf.Value = 42;
        Assert.Equal("fallback", context.GetRequired<string>());
        Assert.Same(outer, context.GetRequired<RefEntry<object>>());
    }

    [Fact]
    public void PublicParsePreservesMissingDiagnostics()
    {
        var leaf = new RefEntry<object>(null);
        var outer = new RefEntry<object>(leaf);
        Assert.False(outer.Parse<string>().Try(out _, out var missing));
        Assert.IsType<EntryValueNotFoundException>(missing.InnerException);
        Assert.Contains("null", missing.Message);

        leaf.Value = 42;
        Assert.False(outer.Parse<string>().Try(out _, out var mismatch));
        Assert.IsType<EntryValueNotFoundException>(mismatch.InnerException);
        Assert.Contains(typeof(int).FullName!, mismatch.Message);
        leaf.Value = "found";
        Assert.True(outer.Parse<string>().Try(out var found, out _));
        Assert.Equal("found", found);
    }

    [Fact]
    public void MutualCycleFailsButDirectWrapperLookupStillSucceeds()
    {
        var first = new RefEntry<object>(null);
        var second = new RefEntry<object>(first);
        first.Value = second;
        using var context = new FlowContext().With("fallback").With(first);

        Assert.Same(first, context.GetRequired<RefEntry<object>>());
        Assert.False(first.Parse<string>().Try(out _, out var parsedError));
        Assert.IsType<InvalidOperationException>(parsedError.InnerException);
        var error = Assert.Throws<ResultException>(() => context.TryGet<string>(out _));
        Assert.IsType<InvalidOperationException>(error.InnerException);
    }
}
