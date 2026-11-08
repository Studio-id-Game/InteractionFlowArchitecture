using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.ExternalPorts;

public interface ICounterOperation : IOperationPort
{
    ValueTask<CounterCommand> ReadAsync(CounterFlowContext context);

    bool Submit(CounterFlowContext context, CounterCommand command);
}
