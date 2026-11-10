using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Lifetime.Entities.Contexts;

public abstract class LifetimeContext : FlowContext, ILifetimeContext
{
    private ILifetimeAccess? lifetime;

    protected LifetimeContext(ILifetimeAccess lifetime)
    {
        this.lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    }

    protected LifetimeContext(ILifetimeContext parent, ContextInheritance inheritance)
        : base(parent, inheritance)
    {
        lifetime = parent.Lifetime;
    }

    public ILifetimeAccess Lifetime => lifetime ?? throw new ObjectDisposedException(GetType().Name);

    protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        if (typeof(T) == typeof(ILifetimeAccess) && Lifetime is T found)
        {
            value = found;
            return true;
        }
        return base.TryGetCore(out value);
    }

    protected override void DisposeCore()
    {
        lifetime = null;
        base.DisposeCore();
    }
}
