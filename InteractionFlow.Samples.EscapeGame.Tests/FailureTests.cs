using InteractionFlow.Core.Entities;
using InteractionFlow.Samples.EscapeGame.Entities;
using System;
using Xunit;

namespace InteractionFlow.Samples.EscapeGame.Tests;

public sealed class FailureTests
{
    [Fact]
    public void SuccessDoesNotBecomeACollectedFailure()
    {
        var failures = new FailureCollector();
        failures.Add(Result.Success);

        Assert.False(failures.HasFailure);
        Assert.True(failures.ToResult().Try(out _));
    }

    [Fact]
    public void SingleFailedResultPreservesItsExistingResultException()
    {
        Result previous = new InvalidOperationException("failed");
        Assert.False(previous.Try(out var original));
        var failures = new FailureCollector();

        failures.Add(previous);
        var result = failures.ToResult();

        Assert.True(failures.HasFailure);
        Assert.False(result.Try(out var actual));
        Assert.Same(original, actual);
    }

    [Fact]
    public void SingleThrownExceptionRemainsTheCauseRatherThanAnAggregate()
    {
        var original = new InvalidOperationException("failed");
        var failures = new FailureCollector();

        failures.Add(original);
        Assert.False(failures.ToResult().Try(out var failure));

        Assert.Same(original, failure!.InnerException);
        Assert.IsNotType<AggregateException>(failure.InnerException);
    }

    [Fact]
    public void MultipleFailuresPreserveOrderWrappersAndNestedAggregates()
    {
        Result first = new FormatException("first");
        Assert.False(first.Try(out var firstWrapper));
        var second = new AggregateException(
            new InvalidOperationException("second-a"),
            new ArgumentException("second-b"));
        Result third = new Exception("third");
        Assert.False(third.Try(out var thirdWrapper));
        var failures = new FailureCollector();

        failures.Add(first);
        failures.Add(Result.Success);
        failures.Add(second);
        failures.Add(third);
        Assert.False(failures.ToResult().Try(out var result));
        var aggregate = Assert.IsType<AggregateException>(result!.InnerException);

        Assert.Equal(3, aggregate.InnerExceptions.Count);
        Assert.Same(firstWrapper, aggregate.InnerExceptions[0]);
        Assert.Same(second, aggregate.InnerExceptions[1]);
        Assert.Same(thirdWrapper, aggregate.InnerExceptions[2]);
        Assert.Equal(2, second.InnerExceptions.Count);
    }

    [Fact]
    public void ThrowingCollectedFailuresUsesTheSameObservationOrder()
    {
        var earlier = new FormatException("earlier");
        var later = new InvalidOperationException("later");
        var failures = new FailureCollector();
        failures.Add(earlier);
        failures.Add(later);

        var aggregate = failures.ToAggregate();

        Assert.Equal(2, aggregate.InnerExceptions.Count);
        Assert.Same(earlier, aggregate.InnerExceptions[0]);
        Assert.Same(later, aggregate.InnerExceptions[1]);
    }

    [Fact]
    public void AppendingToSuccessReturnsTheSameException()
    {
        var next = new InvalidOperationException("next");

        Assert.Same(next, FailureCollector.Append(Result.Success, next));
    }

    [Fact]
    public void RepeatedAppendKeepsExistingNestedAggregateStructureAndOrder()
    {
        Result previous = new FormatException("first");
        Assert.False(previous.Try(out var firstWrapper));
        var second = new InvalidOperationException("second");
        var third = new ArgumentException("third");

        var inner = Assert.IsType<AggregateException>(FailureCollector.Append(previous, second));
        Result combined = inner;
        Assert.False(combined.Try(out var combinedWrapper));
        var outer = Assert.IsType<AggregateException>(FailureCollector.Append(combined, third));

        Assert.Equal(2, inner.InnerExceptions.Count);
        Assert.Same(firstWrapper, inner.InnerExceptions[0]);
        Assert.Same(second, inner.InnerExceptions[1]);
        Assert.Equal(2, outer.InnerExceptions.Count);
        Assert.Same(combinedWrapper, outer.InnerExceptions[0]);
        Assert.Same(inner, combinedWrapper!.InnerException);
        Assert.Same(third, outer.InnerExceptions[1]);
    }

    [Fact]
    public void CollectorDoesNotAlterPreviouslyCreatedAggregateWhenMoreFailuresAreAdded()
    {
        var first = new FormatException("first");
        var second = new InvalidOperationException("second");
        var third = new ArgumentException("third");
        var failures = new FailureCollector();
        failures.Add(first);
        failures.Add(second);
        var earlier = failures.ToAggregate();

        failures.Add(third);
        var later = failures.ToAggregate();

        Assert.Equal(2, earlier.InnerExceptions.Count);
        Assert.Equal(3, later.InnerExceptions.Count);
        Assert.Same(first, later.InnerExceptions[0]);
        Assert.Same(second, later.InnerExceptions[1]);
        Assert.Same(third, later.InnerExceptions[2]);
    }
}
