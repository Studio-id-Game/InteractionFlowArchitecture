using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Raylib.Entities;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.Externals;

internal sealed class CounterExceptionHandling : ExceptionHandling
{
    public CounterExceptionHandling()
    {
    }

    public override void ForceResetMemoryState() => ThrowException = false;

    protected override ValueTask<ReactionEnd> HandleExceptionCoreAsync(IFlowContext context, Exception exception)
    {
        Result result = exception;

        // Close から送出された例外も、既に集約したフローの失敗を上書きしません。
        if (context.TryGet<Result>(out var preceding) && !preceding.Try(out var previous))
            result = new AggregateException(previous, exception);
        context.GetRequired<CounterDisplayState>().Show(
            context.GetRequired<CounterState>().Count,
            "Error: " + exception.Message);

        return new(GetEnd(result));
    }
}
