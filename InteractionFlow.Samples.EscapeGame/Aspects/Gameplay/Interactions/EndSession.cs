using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;

public sealed class EndSession(
    IGameSessionReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<IGameplayContext>(exceptionPort, cancellationPort, reaction)
{
    protected override ValueTask<ReactionEnd> ExecuteCoreAsync(IGameplayContext context)
    {
        var finishing = context.GetRequired<CleanupInfo>();
        if (finishing.Kind != CleanupKind.EndSession)
            throw new InvalidOperationException("EndSession requires matching finishing information.");

        return new(reaction.EndSession(context, finishing.PreviousResult));
    }
}
