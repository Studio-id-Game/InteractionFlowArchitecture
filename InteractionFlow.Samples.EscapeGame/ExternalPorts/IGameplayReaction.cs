using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.ExternalPorts;

public interface IGameplayReaction : IReactionPort
{
    ValueTask<ReactionEnd> BeginSessionAsync(IGameplayContext context);
    ValueTask<ReactionEnd> BeginActionSelectionAsync(IGameplayContext context);
    ValueTask<ReactionEnd> HandleCommandAsync(IGameplayContext context, GameCommand command);
    ValueTask<ReactionEnd> ApplyPlayerActionAsync(IGameplayContext context);
    ValueTask<ReactionEnd> ApplyEnemyActionAsync(IGameplayContext context);
    ValueTask<ReactionEnd> PresentResultAsync(IGameplayContext context);
    ValueTask<ReactionEnd> EnableResultInputAsync(IGameplayContext context);
}
