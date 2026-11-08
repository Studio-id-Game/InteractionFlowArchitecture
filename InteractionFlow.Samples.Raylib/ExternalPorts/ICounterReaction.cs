using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.ExternalPorts;

public interface ICounterReaction : IReactionPort
{
    ValueTask<ReactionEnd> RespondAsync(CounterFlowContext context, CounterCommand command);
}
