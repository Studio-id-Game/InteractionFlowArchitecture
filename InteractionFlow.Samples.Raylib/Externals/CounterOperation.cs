using InteractionFlow.Samples.Raylib.Entities.Contexts;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Raylib.Entities;
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

    public bool Submit(IFlowContext context, CounterCommand command)
        => context.GetRequired<ICounterInputAccess>().TrySubmit(command);

    public ValueTask<CounterCommand> ReadAsync(IFlowContext context)
        => context.GetRequired<ICounterInputAccess>().ReadCommandAsync(context.Cancellation.GetToken());
}
