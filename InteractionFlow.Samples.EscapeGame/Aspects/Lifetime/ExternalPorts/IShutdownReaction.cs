using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.ExternalPorts;

public interface IShutdownReaction : IReactionPort
{
    ReactionEnd RequestShutdown(ILifetimeContext context);
}
