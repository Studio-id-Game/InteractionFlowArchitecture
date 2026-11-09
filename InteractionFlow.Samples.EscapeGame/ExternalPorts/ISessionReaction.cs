using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.ExternalPorts;

public interface ISessionReaction : IReactionPort
{
    ReactionEnd EndSession(EscapeApplicationContext context, Result previous);
    ReactionEnd RequestShutdown(EscapeApplicationContext context);
}
