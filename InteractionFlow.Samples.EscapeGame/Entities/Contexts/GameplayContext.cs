using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

public sealed class GameplayContext : FlowContext, IGameplayContext
{
    private IGameplayAccess? gameplay;
    private IApplicationLifetimeAccess? lifetime;

    // アプリからはCancellationのみ、同じ役割の子はAll、後始末はNoneを指定します。
    public GameplayContext(IGameplayContext parent, ContextInheritance inheritance)
        : this(parent, parent?.SessionId ?? throw new ArgumentNullException(nameof(parent)), inheritance) { }

    public GameplayContext(IGameplayContext parent, long sessionId, ContextInheritance inheritance)
        : base(parent, inheritance)
    {
        gameplay = parent.Gameplay;
        lifetime = parent.Lifetime;
        SessionId = sessionId;
    }

    public long SessionId { get; }
    public IGameplayAccess Gameplay => gameplay ?? throw new ObjectDisposedException(nameof(GameplayContext));
    public IApplicationLifetimeAccess Lifetime => lifetime ?? throw new ObjectDisposedException(nameof(GameplayContext));

    protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        object? provided = typeof(T) == typeof(IGameplayAccess) ? Gameplay
            : typeof(T) == typeof(IApplicationLifetimeAccess) ? Lifetime : null;
        if (provided is T found)
        {
            value = found;
            return true;
        }
        // 親の値探索を選んだ場合も、別の役割の固定Accessは公開しません。
        if (typeof(T) == typeof(IGameViewAccess))
        {
            value = default;
            return false;
        }
        return base.TryGetCore(out value);
    }

    protected override void ValidateAdditionalValue<T>(T value) => ContextAccessValidation.Validate(value!);

    protected override void DisposeCore()
    {
        gameplay = null;
        lifetime = null;
    }
}
