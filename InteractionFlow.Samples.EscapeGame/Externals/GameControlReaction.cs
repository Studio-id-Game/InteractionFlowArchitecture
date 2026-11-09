using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;

namespace InteractionFlow.Samples.EscapeGame.Externals;

public sealed class GameControlReaction : Reaction, IGameControlReaction
{
    public override void ForceResetMemoryState() { }

    public ReactionEnd Change(WindowContext context)
    {
        var frame = context.GetRequired<FrameState>();
        if (frame.PauseRequested)
        {
            var progress = context.Session.GetProgress(frame.SessionId);
            context.Control.TrySetPaused(frame.SessionId, !progress.Paused);
        }
        else if (frame.SkipRequested && frame.Presentation is PresentationKey key)
        {
            context.Presentation.TrySkip(key);
        }

        return GetEnd();
    }
}
