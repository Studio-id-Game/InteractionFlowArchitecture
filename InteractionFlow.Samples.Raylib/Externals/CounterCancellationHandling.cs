using InteractionFlow.Samples.Raylib.Entities.Contexts;
using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Raylib.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.Externals;

internal sealed class CounterCancellationHandling : CancellationHandling
{
    public CounterCancellationHandling()
    {
    }

    public override void ForceResetMemoryState() => ThrowException = false;

    protected override ValueTask<ReactionEnd> AfterCancellationCoreAsync(
        IFlowContext context,
        OperationCanceledException exception,
        Result waitAndResetResult)
    {
        context.GetRequired<ICounterStateAccess>().Cancel();

        // Close の中断は再入力へ回復せず、先行結果とともに終了失敗として保持します。
        if (context.TryGet<Result>(out var preceding))
        {
            var failures = new List<Exception> { exception };

            if (!preceding.Try(out var previous))
                failures.Insert(0, previous);

            if (!waitAndResetResult.Try(out var waiting))
                failures.Add(waiting);
            return new(GetEnd(failures.Count == 1 ? failures[0] : new AggregateException(failures)));
        }

        return new(GetEnd(waitAndResetResult));
    }
}
