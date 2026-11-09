using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.ExternalPorts;

public interface IGameReaction : IReactionPort
{
    ValueTask<ReactionEnd> BeginAsync(EscapeApplicationContext context);
    ValueTask<ReactionEnd> EnableSelectionAsync(EscapeApplicationContext context);
    ValueTask<ReactionEnd> SelectAsync(EscapeApplicationContext context, GameCommand command);
    ValueTask<ReactionEnd> ApplyPlayerAsync(EscapeApplicationContext context);
    ValueTask<ReactionEnd> ApplyEnemyAsync(EscapeApplicationContext context);
    ValueTask<ReactionEnd> ShowResultAsync(EscapeApplicationContext context);
    ValueTask<ReactionEnd> EnableResultInputAsync(EscapeApplicationContext context);
}
