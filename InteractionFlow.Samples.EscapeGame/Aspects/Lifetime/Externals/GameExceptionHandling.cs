using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Externals;

public sealed class GameExceptionHandling : ExceptionHandling
{
    public override void ForceResetMemoryState() => ThrowException = false;

    protected override ValueTask<ReactionEnd> HandleExceptionCoreAsync(IFlowContext context, Exception exception)
    {
        Result result = exception;
        if (context.TryGet<CleanupInfo>(out var finishing))
            result = FailureCollector.Append(finishing.PreviousResult, exception);

        try { context.GetRequired<ILifetimeAccess>().RequestShutdown(); }
        catch (Exception stopFailure)
        {
            result = FailureCollector.Append(result, stopFailure);
        }

        return new(GetEnd(result));
    }
}
