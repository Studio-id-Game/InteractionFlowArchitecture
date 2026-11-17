using InteractionFlow.Core.ResultAPI.Entities;
using System;
using System.Collections.Generic;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Entities;

// 終了時の失敗を観測順に保持します。Result の通常の成功連結とは区別します。
internal sealed class FailureCollector
{
    private List<Exception>? failures;

    public bool HasFailure => failures is { Count: > 0 };

    public Exception Add(Exception failure)
    {
        failures ??= [];
        failures.Add(failure);

        return failure;
    }

    public Result Add(Result result)
    {
        if (result.HasError(out var failure))
        {
            Add(failure);
        }

        return result;
    }

    // 終了手順だけを継続可能にします。記録した失敗はToResultで最終結果へ戻します。
    public Result RecordAndContinue(Exception failure)
    {
        Add(failure);
        return Result.Success;
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
        => previous.HasError(out var earlier) ? new AggregateException(earlier, next) : next;
}
