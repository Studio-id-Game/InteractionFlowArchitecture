using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.Externals;

internal sealed class CounterOperation : Operation, ICounterOperation
{
    private readonly object gate = new();
    private TaskCompletionSource<CounterCommand>? pending;
    private CounterDisplayState? pendingDisplay;

    public CounterOperation()
    {
    }

    public override void ForceResetMemoryState()
    {
        lock (gate)
        {
            if (pending is not null)
                throw new InvalidOperationException("Cannot reset an active input wait.");
        }
    }

    public bool Submit(CounterCommand command)
    {
        lock (gate)
        {
            if (pending is null || !pending.TrySetResult(command))
                return false;

            pendingDisplay!.SetAcceptingInput(false);

            return true;
        }
    }

    public async ValueTask<CounterCommand> ReadAsync(IFlowContext context)
    {
        var token = context.Cancellation.GetToken();
        var display = context.GetRequired<CounterDisplayState>();
        var source = new TaskCompletionSource<CounterCommand>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (gate)
        {
            if (pending is not null)
                throw new InvalidOperationException("Only one input wait is supported.");

            display.SetAcceptingInput(true);
            pending = source;
            pendingDisplay = display;
        }

        try
        {
            return await source.Task.WaitAsync(token).ConfigureAwait(false);
        }
        finally
        {
            lock (gate)
            {
                pending = null;
                pendingDisplay = null;
                display.SetAcceptingInput(false);
            }
        }
    }
}
