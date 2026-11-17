using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Core.ResultAPI.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Externals;

public sealed class GameCancellationHandling : CancellationHandling
{
    public override void ForceResetMemoryState() => ThrowException = false;

    protected override ValueTask<ReactionEnd> AfterCancellationCoreAsync(
        IFlowContext context,
        OperationCanceledException exception,
        Result waitAndResetResult)
    {
        var failures = new FailureCollector();
        var finishing = context.TryGet<CleanupInfo>(out var operation);
        if (finishing && operation!.PreviousResult.HasError(out var earlierFailure))
            failures.Add(earlierFailure);

        var waitFailure = waitAndResetResult.Error;
        var resetSucceeded = waitFailure is null;
        // 通常の停止通知は回復できますが、Close/EndSession 自身のキャンセルは失敗です。
        if (finishing || !resetSucceeded)
            failures.Add(exception);
        if (!resetSucceeded)
            failures.Add(waitFailure!);

        try { context.GetRequired<ILifetimeAccess>().RequestShutdown(); }
        catch (Exception error)
        {
            failures.Add(error);
        }

        return new(GetEnd(failures.ToResult()));
    }
}
