using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Raylib.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.ExternalPorts;

public interface ICounterReaction : IReactionPort
{
    ValueTask<ReactionEnd> RespondAsync(IFlowContext context, CounterCommand command);
}
