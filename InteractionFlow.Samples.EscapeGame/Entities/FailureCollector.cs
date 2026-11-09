using InteractionFlow.Core.Entities;
using System;
using System.Collections.Generic;

namespace InteractionFlow.Samples.EscapeGame.Entities;

// 終了時の失敗を観測順に保持します。Result の通常の成功連結とは区別します。
internal sealed class FailureCollector
{
    private List<Exception>? failures;

    public bool HasFailure => failures is { Count: > 0 };

    public void Add(Exception failure)
    {
        failures ??= [];
        failures.Add(failure);
    }

    public void Add(Result result)
    {
        if (!result.Try(out var failure))
            Add(failure);
    }

    public Result ToResult()
    {
        if (failures is null)
            return Result.Success;

        return failures.Count switch
        {
            0 => Result.Success,
            1 => failures[0],
            _ => ToAggregate()
        };
    }

    public AggregateException ToAggregate()
        => new(failures ?? (IEnumerable<Exception>)Array.Empty<Exception>());

    // 既存の ResultException を内側の一要素として保ち、flatten / 再ラップしません。
    public static Exception Append(Result previous, Exception next)
        => previous.Try(out var earlier) ? next : new AggregateException(earlier, next);
}
