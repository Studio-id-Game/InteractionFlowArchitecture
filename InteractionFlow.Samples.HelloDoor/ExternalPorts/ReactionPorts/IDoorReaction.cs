using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.ExternalPorts.ReactionPorts
{
    internal interface IDoorReaction : IReactionPort
    {
        ValueTask<ReactionEnd> WriteAsync(IFlowContext context, string message);
    }
}
