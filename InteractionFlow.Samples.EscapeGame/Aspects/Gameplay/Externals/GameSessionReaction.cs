using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Externals;

public sealed class GameSessionReaction : Reaction, IGameSessionReaction
{
    public override void ForceResetMemoryState() { }

    public ReactionEnd EndSession(IGameplayContext context, Result previous)
    {
        try { context.Gameplay.EndSession(context.SessionId); }
        catch (Exception error)
        {
            previous = FailureCollector.Append(previous, error);
        }

        return GetEnd(previous);
    }
}
