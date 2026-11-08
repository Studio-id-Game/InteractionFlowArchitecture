using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.Entities.Contexts;
using InteractionFlow.Samples.Raylib.ExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.Externals;

internal sealed class CounterOperation : Operation, ICounterOperation
{
    public CounterOperation()
    {
    }

    public override void ForceResetMemoryState()
    {
    }

    public bool Submit(CounterFlowContext context, CounterCommand command)
        => context.Input.TrySubmit(command);

    public ValueTask<CounterCommand> ReadAsync(CounterFlowContext context)
        => context.Input.ReadCommandAsync(context.Cancellation.GetToken());
}
