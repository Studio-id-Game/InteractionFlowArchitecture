using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.View.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System.Threading.Tasks;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.View.Interactions;

public sealed class PresentFrame(
    IWindowOperation operation,
    IWindowReaction reaction,
    IGameInputOperation game,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<IViewContext>(exceptionPort, cancellationPort, operation, reaction, game)
{
    protected override ValueTask<ReactionEnd> ExecuteCoreAsync(IViewContext context)
    {
        operation.PollFrameInput(context);
        var input = context.GetRequired<FrameState>();

        if (!input.ShutdownRequested && !input.PauseRequested && !input.SkipRequested
            && input.Command is GameCommand command)
        {
            game.TrySubmitCommand(context, input.SessionId, command);
        }

        return new(reaction.DrawFrame(context));
    }
}
