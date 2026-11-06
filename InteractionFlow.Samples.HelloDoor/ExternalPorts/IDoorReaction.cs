using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.HelloDoor.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.ExternalPorts
{
    public interface IDoorReaction : IReactionPort
    {
        ValueTask<ReactionEnd> ReactDoorAsync(IFlowContext context, DoorCommand command);
    }
}
