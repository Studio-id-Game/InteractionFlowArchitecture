using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.ExternalPorts;

public interface IWindowReaction : IReactionPort
{
    ReactionEnd Open(IViewContext context);
    ReactionEnd DrawFrame(IViewContext context);
    ReactionEnd Close(ILifetimeContext context, Result previous);
}
