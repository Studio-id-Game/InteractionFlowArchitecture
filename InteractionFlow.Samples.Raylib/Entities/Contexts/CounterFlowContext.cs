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

    // アクセス管理だけを共有する別ルートです。Cancellation と追加値は継承しません。
    public CounterFlowContext CreateIndependentContext()
    {
        _ = this.GetRequired<ICounterInputAccess>(); // 基底の破棄済みチェックも通します。
        return new CounterFlowContext(access!);
    }

    protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
    {
        if ((typeof(T) == typeof(ICounterInputAccess)
            || typeof(T) == typeof(ICounterStateAccess)
            || typeof(T) == typeof(ICounterDisplayAccess)) && access is T found)
        {
            value = found;
            return true;
        }

        return base.TryGetCore(out value);
    }

    // 共有値の所有権は取得しません。キャンセル・待機・共有状態の破棄も行いません。
    protected override void DisposeCore() => access = null;
}
