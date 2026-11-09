using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Interactions;

public sealed class EndSession(
    IGameLifetimeReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<IGameplayContext>(exceptionPort, cancellationPort, reaction)
{
    protected override ValueTask<ReactionEnd> ExecuteCoreAsync(IGameplayContext context)
    {
        var finishing = context.GetRequired<Finishing>();
        if (finishing.Kind != FinishingKind.EndSession)
            throw new InvalidOperationException("EndSession requires matching finishing information.");

        return new(reaction.EndSession(context, finishing.Previous));
    }
}
