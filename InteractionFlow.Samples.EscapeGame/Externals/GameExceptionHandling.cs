using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities;
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
        if (context.TryGet<CleanupInfo>(out var finishing))
            result = FailureCollector.Append(finishing.PreviousResult, exception);

        try { context.GetRequired<IApplicationLifetimeAccess>().RequestShutdown(); }
        catch (Exception stopFailure)
        {
            result = FailureCollector.Append(result, stopFailure);
        }

        return new(GetEnd(result));
    }
}
