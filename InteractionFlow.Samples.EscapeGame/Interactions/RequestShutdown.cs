using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Interactions;

public sealed class RequestShutdown(
    IGameLifetimeReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<ILifetimeContext>(exceptionPort, cancellationPort, reaction)
{
    protected override ValueTask<ReactionEnd> ExecuteCoreAsync(ILifetimeContext context)
        => new(reaction.RequestShutdown(context));
}
