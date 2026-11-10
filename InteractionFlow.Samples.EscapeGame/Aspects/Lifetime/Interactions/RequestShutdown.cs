using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Interactions;

public sealed class RequestShutdown(
    IGameLifetimeReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<ILifetimeContext>(exceptionPort, cancellationPort, reaction)
{
    protected override ValueTask<ReactionEnd> ExecuteCoreAsync(ILifetimeContext context)
        => new(reaction.RequestShutdown(context));
}
