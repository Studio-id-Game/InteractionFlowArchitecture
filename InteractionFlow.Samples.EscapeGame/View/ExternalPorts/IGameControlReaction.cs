using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.View.ExternalPorts;

public interface IGameControlReaction : IReactionPort
{
    ReactionEnd ApplyControlRequest(IGameViewContext context);
}
