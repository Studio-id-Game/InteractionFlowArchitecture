using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.Aspects.View.ExternalPorts;

public interface IGameControlReaction : IReactionPort
{
    ReactionEnd ApplyControlRequest(IViewContext context);
}
