using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.ExternalPorts;

public interface IWindowReaction : IReactionPort
{
    ReactionEnd Open(IGameViewContext context);
    ReactionEnd Draw(IGameViewContext context);
    ReactionEnd Close(ILifetimeContext context, Result previous);
}
