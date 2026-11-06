using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.HelloDoor.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.ExternalPorts
{
    public interface IDoorOperation : IOperationPort
    {
        ValueTask<DoorCommand> ReadDoorCommandAsync(IFlowContext context);
    }
}
