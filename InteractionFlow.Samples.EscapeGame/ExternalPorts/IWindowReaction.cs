using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.ExternalPorts;

public interface IWindowReaction : IReactionPort
{
    ReactionEnd Open(EscapeApplicationContext context);
    ReactionEnd Draw(EscapeApplicationContext context);
    ReactionEnd Close(EscapeApplicationContext context, Result previous);
}
