using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.View.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.Aspects.View.Externals;

public sealed class GameControlReaction : Reaction, IGameControlReaction
{
    public override void ForceResetMemoryState() { }

    public ReactionEnd ApplyControlRequest(IViewContext context)
    {
        var frame = context.GetRequired<FrameState>();
        if (frame.PauseRequested)
        {
            var progress = context.View.GetSessionProgress(frame.SessionId);
            context.View.TrySetPaused(frame.SessionId, !progress.Paused);
        }
        else if (frame.SkipRequested && frame.Presentation is PresentationKey key)
        {
            context.View.TryRequestPresentationSkip(key);
        }

        return GetEnd();
    }
}
