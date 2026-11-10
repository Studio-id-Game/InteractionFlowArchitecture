using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Externals;

public sealed class GameLifetimeReaction : Reaction, IGameLifetimeReaction
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

    public ReactionEnd RequestShutdown(ILifetimeContext context)
    {
        // 停止・入力閉鎖・提示解除・再開待機解除は同じ共有アクセス境界で行います。
        context.Lifetime.RequestShutdown();
        return GetEnd();
    }
}
