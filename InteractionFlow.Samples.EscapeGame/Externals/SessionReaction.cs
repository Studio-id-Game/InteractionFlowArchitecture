using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Entities;
using InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.ExternalPorts;
using System;

namespace InteractionFlow.Samples.EscapeGame.Externals;

public sealed class SessionReaction : Reaction, ISessionReaction
{
    public override void ForceResetMemoryState() { }

    public ReactionEnd EndSession(EscapeApplicationContext context, Result previous)
    {
        try { context.Gameplay.EndSession(context.SessionId); }
        catch (Exception error)
        {
            previous = FailureCollector.Append(previous, error);
        }

        return GetEnd(previous);
    }

    public ReactionEnd RequestShutdown(EscapeApplicationContext context)
    {
        // 停止・入力閉鎖・提示解除・再開待機解除は同じ共有アクセス境界で行います。
        context.Lifetime.RequestShutdown();
        return GetEnd();
    }
}
