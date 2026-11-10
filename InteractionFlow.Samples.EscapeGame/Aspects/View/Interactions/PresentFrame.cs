using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.View.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.View.Interactions;

public sealed class PresentFrame(
    IWindowOperation operation,
    IWindowReaction reaction,
    IGameInputSubmissionOperation game,
    IExceptionPort<Exception> exceptionPort,
    ICancellationPort cancellationPort)
    : Interaction<IViewContext>(exceptionPort, cancellationPort, operation, reaction, game)
{
    protected override ValueTask<ReactionEnd> ExecuteCoreAsync(IViewContext context)
    {
        operation.PollFrameInput(context);
        var input = context.GetRequired<FrameState>();

        if (!input.ShutdownRequested)
        {
            if (input.PauseRequested)
                game.TrySubmitControl(context, new(input.SessionId, GameControlCommand.TogglePause));
            else if (input.SkipRequested)
                game.TrySubmitControl(context, new(input.SessionId, GameControlCommand.Skip, input.Presentation));
            else if (input.Command is GameCommand command)
                game.TrySubmitCommand(context, input.SessionId, command);
        }

        return new(reaction.DrawFrame(context));
    }
}
