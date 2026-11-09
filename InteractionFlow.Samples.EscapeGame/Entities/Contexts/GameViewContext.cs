using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

public sealed class GameViewContext : FlowContext, IGameViewContext
{
    private IGameViewAccess? view;
    private IApplicationLifetimeAccess? lifetime;

    public GameViewContext(IGameViewContext parent, ContextInheritance inheritance) : base(parent, inheritance)
    {
        view = parent.View;
        lifetime = parent.Lifetime;
    }

    public IGameViewAccess View => view ?? throw new ObjectDisposedException(nameof(GameViewContext));
    public IApplicationLifetimeAccess Lifetime => lifetime ?? throw new ObjectDisposedException(nameof(GameViewContext));

    protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        object? provided = typeof(T) == typeof(IGameViewAccess) ? View
            : typeof(T) == typeof(IApplicationLifetimeAccess) ? Lifetime : null;
        if (provided is T found)
        {
            value = found;
            return true;
        }
        // 親の値探索を選んだ場合も、別の役割の固定Accessは公開しません。
        if (typeof(T) == typeof(IGameplayAccess))
        {
            value = default;
            return false;
        }
        return base.TryGetCore(out value);
    }

    protected override void ValidateAdditionalValue<T>(T value) => ContextAccessValidation.Validate(value!);

    protected override void DisposeCore()
    {
        view = null;
        lifetime = null;
    }
}
