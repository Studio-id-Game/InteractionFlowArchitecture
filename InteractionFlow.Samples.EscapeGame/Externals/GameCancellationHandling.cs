using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Externals;

public sealed class GameCancellationHandling : CancellationHandling
{
    public override void ForceResetMemoryState() => ThrowException = false;

    protected override ValueTask<ReactionEnd> AfterCancellationCoreAsync(
        IFlowContext context,
        OperationCanceledException exception,
        Result waitAndResetResult)
    {
        var failures = new List<Exception>();
        var finishing = context.TryGet<Finishing>(out var operation);
        if (finishing && !operation!.Previous.Try(out var earlierFailure))
            failures.Add(earlierFailure);

        var resetSucceeded = waitAndResetResult.Try(out var waitFailure);
        // 通常の停止通知は回復できますが、Close/EndSession 自身のキャンセルは失敗です。
        if (finishing || !resetSucceeded)
            failures.Add(exception);
        if (!resetSucceeded)
            failures.Add(waitFailure!);

        try { context.GetRequired<IGameWorldAccess>().RequestShutdown(); }
        catch (Exception error)
        {
            failures.Add(error);
        }

        Result result = failures.Count switch
        {
            0 => Result.Success,
            1 => failures[0],
            _ => new AggregateException(failures)
        };
        return new(GetEnd(result));
    }
}
