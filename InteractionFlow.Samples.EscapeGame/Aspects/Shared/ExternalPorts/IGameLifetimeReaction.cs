using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.ExternalPorts;

public interface IGameLifetimeReaction : IReactionPort
{
    ReactionEnd EndSession(IGameplayContext context, Result previous);
    ReactionEnd RequestShutdown(ILifetimeContext context);
}
