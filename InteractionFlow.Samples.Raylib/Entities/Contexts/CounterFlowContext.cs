using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Samples.Raylib.Entities.Contexts;

// 値のアクセス契約を提供します。進行判断・Task 実行・待機は担当しません。
public sealed class CounterFlowContext : FlowContext
{
    private CounterAccess? access;

    public CounterFlowContext()
    {
        access = new();
    }

    private CounterFlowContext(CounterAccess shared)
    {
        access = shared;
    }

    public CounterFlowContext(CounterFlowContext parent) : base(parent)
    {
        access = parent.Access;
    }

    private CounterAccess Access => access ?? throw new ObjectDisposedException(nameof(CounterFlowContext));

    public ICounterInputAccess Input => Access;

    public ICounterStateAccess State => Access;

    public ICounterDisplayAccess Display => Access;

    // アクセス管理だけを共有する別ルートです。Cancellation と追加値は継承しません。
    public CounterFlowContext CreateIndependentContext()
    {
        return new CounterFlowContext(Access);
    }

    protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        if (typeof(T) == typeof(ICounterInputAccess))
        {
            value = (T)(object)Input;
            return true;
        }

        if (typeof(T) == typeof(ICounterStateAccess))
        {
            value = (T)(object)State;
            return true;
        }

        if (typeof(T) == typeof(ICounterDisplayAccess))
        {
            value = (T)(object)Display;
            return true;
        }

        return base.TryGetCore(out value);
    }

    protected override void ValidateAdditionalValue<T>(T value)
    {
        // Entry の後日の書き換えでも固定アクセス契約を隠蔽できないよう、一律に拒否します。
        if (value is ICounterInputAccess or ICounterStateAccess or ICounterDisplayAccess || IsEntry(value!))
            throw new ArgumentException("Counter access contracts and Entry values cannot be added.", nameof(value));
    }

    private static bool IsEntry(object value)
    {
        for (var type = value.GetType(); type is not null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Entry<>))
                return true;
        }

        return false;
    }

    // 共有値の所有権は取得しません。キャンセル・待機・共有状態の破棄も行いません。
    protected override void DisposeCore() => access = null;
}
