using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.OperationPorts;
using InteractionFlow.Samples.HelloDoor.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.ExternalPorts.OperationPorts
{
    internal interface IDoorOperation : IOperationPort
    {
        ValueTask<DoorCommand> ReadCommandAsync(IFlowContext context);
    }
}
