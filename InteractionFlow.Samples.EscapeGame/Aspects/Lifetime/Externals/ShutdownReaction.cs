using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Externals;

public sealed class ShutdownReaction : Reaction, IShutdownReaction
{
    public override void ForceResetMemoryState() { }

    public ReactionEnd RequestShutdown(ILifetimeContext context)
    {
        // 停止・入力閉鎖・提示解除・再開待機解除は同じ共有アクセス境界で行います。
        context.Lifetime.RequestShutdown();
        return GetEnd();
    }
}
