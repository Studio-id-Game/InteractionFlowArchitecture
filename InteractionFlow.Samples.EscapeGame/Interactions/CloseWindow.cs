using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Interactions;

public sealed class CloseWindow(
    IWindowReaction reaction,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<ILifetimeContext>(exceptionPort, cancellationPort, reaction)
{
    protected override ValueTask<ReactionEnd> ExecuteCoreAsync(ILifetimeContext context)
    {
        var finishing = context.GetRequired<CleanupInfo>();
        if (finishing.Kind != CleanupKind.CloseWindow)
            throw new InvalidOperationException("CloseWindow requires matching finishing information.");

        return new(reaction.Close(context, finishing.PreviousResult));
    }
}
