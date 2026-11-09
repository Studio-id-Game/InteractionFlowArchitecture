using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

// 値の探索先とCancellationの共有元を別々に選ぶ、サンプル内の実験実装です。
public abstract class EscapeContext : IFlowContext, IDisposable
{
    private readonly ValuesContext values;

    protected EscapeContext() : this(new CancellationObject(), null) { }

    protected EscapeContext(CancellationObject cancellation) : this(cancellation, null) { }

    protected EscapeContext(EscapeContext parent)
        : this(parent?.Cancellation ?? throw new ArgumentNullException(nameof(parent)), parent) { }

    private EscapeContext(CancellationObject cancellation, IFlowContext? parent)
    {
        values = new ValuesContext(this, cancellation, parent);
    }

    // Disposeは共有Cancellationを停止・リセットしません。破棄後も参照できます。
    public CancellationObject Cancellation => values.Cancellation;

    public EscapeContext With<T>(T value)
    {
        values.With(value);
        return this;
    }

    public bool TryGet<T>([MaybeNullWhen(false)] out T value) => values.TryGet(out value);

    protected virtual bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        value = default;
        return false;
    }

    protected virtual void ValidateAdditionalValue<T>(T value)
        => EscapeContextValues.ValidateAdditional(value!);

    public void Dispose() => values.Dispose();

    protected abstract void DisposeCore();

    // 追加値・Entry解決・破棄チェックは既存FlowContextへ委譲します。
    // 探索順は「追加値→自身の固定値→同種の親→Cancellation」を維持します。
    private sealed class ValuesContext(
        EscapeContext owner,
        CancellationObject cancellation,
        IFlowContext? parent) : FlowContext(new CancellationContext(cancellation))
    {
        private IFlowContext? inheritedValues = parent;

        protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
        {
            if (owner.TryGetCore(out value))
                return true;
            if (inheritedValues is not null)
                return inheritedValues.TryGet(out value);
            return base.TryGetCore(out value);
        }

        protected override void ValidateAdditionalValue<T>(T value)
            => owner.ValidateAdditionalValue(value);

        protected override void DisposeCore()
        {
            inheritedValues = null;
            owner.DisposeCore();
        }
    }

    // 元Contextを保持・探索せず、Cancellationだけを接続します。
    private sealed class CancellationContext(CancellationObject cancellation) : IFlowContext
    {
        public CancellationObject Cancellation { get; } = cancellation
            ?? throw new ArgumentNullException(nameof(cancellation));

        public bool TryGet<T>([MaybeNullWhen(false)] out T value)
        {
            if (Cancellation is T found)
            {
                value = found;
                return true;
            }
            value = default;
            return false;
        }
    }
}
