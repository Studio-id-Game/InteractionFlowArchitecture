using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Interactions;

public sealed class PresentFrame(
    IWindowOperation operation,
    IWindowReaction reaction,
    IGameInputOperation game,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<IGameViewContext>(exceptionPort, cancellationPort, operation, reaction, game)
{
    protected override ValueTask<ReactionEnd> ExecuteCoreAsync(IGameViewContext context)
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
