using InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using System.Diagnostics.CodeAnalysis;
using System;

namespace InteractionFlow.Samples.EscapeGame.Aspects.View.Entities.Contexts;

public sealed class ViewContext : LifetimeContext, IViewContext
{
    private IViewAccess? view;

    public ViewContext(IViewContext parent, ContextInheritance inheritance) : base(parent, inheritance)
    {
        view = parent.View;
    }

    public IViewAccess View => view ?? throw new ObjectDisposedException(nameof(ViewContext));

    protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        object? provided = typeof(T) == typeof(IViewAccess) ? View : null;
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
        base.DisposeCore();
    }
}
