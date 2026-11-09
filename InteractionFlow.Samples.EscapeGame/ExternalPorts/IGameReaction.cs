using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.ExternalPorts;

public interface IGameReaction : IReactionPort
{
    ValueTask<ReactionEnd> BeginAsync(IGameplayContext context);
    ValueTask<ReactionEnd> EnableSelectionAsync(IGameplayContext context);
    ValueTask<ReactionEnd> SelectAsync(IGameplayContext context, GameCommand command);
    ValueTask<ReactionEnd> ApplyPlayerAsync(IGameplayContext context);
    ValueTask<ReactionEnd> ApplyEnemyAsync(IGameplayContext context);
    ValueTask<ReactionEnd> ShowResultAsync(IGameplayContext context);
    ValueTask<ReactionEnd> EnableResultInputAsync(IGameplayContext context);
}
