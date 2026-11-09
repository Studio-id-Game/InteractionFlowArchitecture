using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Externals;

public sealed class GameExceptionHandling : ExceptionHandling
{
    public override void ForceResetMemoryState() => ThrowException = false;

    protected override ValueTask<ReactionEnd> HandleExceptionCoreAsync(IFlowContext context, Exception exception)
    {
        Result result = exception;
        if (context.TryGet<Result>(out var previous) && !previous.Try(out var earlierFailure))
            result = new AggregateException(earlierFailure, exception);

        try { context.GetRequired<IGameWorldAccess>().RequestShutdown(); }
        catch (Exception stopFailure)
        {
            result = result.Try(out var earlier) ? stopFailure : new AggregateException(earlier, stopFailure);
        }

        return new(GetEnd(result));
    }
}
