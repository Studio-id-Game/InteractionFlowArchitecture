using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Application.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using System;

namespace InteractionFlow.Samples.EscapeGame.Application.Externals;

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
