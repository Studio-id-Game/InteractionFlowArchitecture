using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Raylib.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.ExternalPorts;

public interface ICounterOperation : IOperationPort
{
    ValueTask<CounterCommand> ReadAsync(IFlowContext context);

    bool Submit(IFlowContext context, CounterCommand command);
}
