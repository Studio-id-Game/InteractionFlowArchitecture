using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.ResultAPI.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;

public interface IGameSessionReaction : IReactionPort
{
    ReactionEnd EndSession(IGameplayContext context, Result previous);
}
