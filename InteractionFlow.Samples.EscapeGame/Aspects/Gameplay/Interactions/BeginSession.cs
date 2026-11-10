using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Interactions;

public sealed class BeginSession(
    IGameplayReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<IGameplayContext>(exceptionPort, cancellationPort, reaction)
{
    protected override ValueTask<ReactionEnd> ExecuteCoreAsync(IGameplayContext context)
        => reaction.BeginSessionAsync(context);
}
