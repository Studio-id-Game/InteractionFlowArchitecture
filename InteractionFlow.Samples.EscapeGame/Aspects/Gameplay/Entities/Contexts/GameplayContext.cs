using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using System.Diagnostics.CodeAnalysis;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities.Contexts;

public sealed class GameplayContext : LifetimeContext, IGameplayContext
{
    private IGameplayAccess? gameplay;

    // アプリからはCancellationのみ、同じ役割の子はAll、後始末はNoneを指定します。
    public GameplayContext(IGameplayContext parent, ContextInheritance inheritance)
        : this(parent, parent?.SessionId ?? throw new ArgumentNullException(nameof(parent)), inheritance) { }

    public GameplayContext(IGameplayContext parent, long sessionId, ContextInheritance inheritance)
        : base(parent, inheritance)
    {
        gameplay = parent.Gameplay;
        SessionId = sessionId;
    }

    public long SessionId { get; }
    public IGameplayAccess Gameplay => gameplay ?? throw new ObjectDisposedException(nameof(GameplayContext));

    protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        object? provided = typeof(T) == typeof(IGameplayAccess) ? Gameplay : null;
        if (provided is T found)
        {
            value = found;
            return true;
        }
        // 親の値探索を選んだ場合も、別の役割の固定Accessは公開しません。
        if (typeof(T) == typeof(IViewAccess))
        {
            value = default;
            return false;
        }
        return base.TryGetCore(out value);
    }

    protected override void DisposeCore()
    {
        gameplay = null;
        base.DisposeCore();
    }
}
