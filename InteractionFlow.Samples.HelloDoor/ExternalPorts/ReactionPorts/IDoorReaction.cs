using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Samples.HelloDoor.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.ExternalPorts.ReactionPorts
{
    internal interface IDoorReaction : IReactionPort
    {
        ValueTask<ReactionEnd> ReactAsync(IFlowContext context, DoorCommand command);
    }
}
