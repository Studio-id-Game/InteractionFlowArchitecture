using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>テンプレートメソッドと、候補の選択・展開に必要な条件を表します。</summary>
internal sealed class TemplateMethod(
    IMethodSymbol method,
    bool usesOther,
    ImmutableArray<IMethodSymbol> requiredFactories,
    bool allowAwaitedReceiver,
    bool allowAwaitedCallbacks)
{
    public IMethodSymbol Method { get; } = method;
    public bool UsesOther { get; } = usesOther;

    /// <summary>テンプレートが参照するファクトリのシンボルを保持します。</summary>
    public ImmutableArray<IMethodSymbol> RequiredFactories { get; } = requiredFactories;

    public bool AllowAwaitedReceiver { get; } = allowAwaitedReceiver;
    public bool AllowAwaitedCallbacks { get; } = allowAwaitedCallbacks;

    /// <summary>Self と Other の参照に基づいて、対象の組を展開候補にできるかを判定します。</summary>
    public bool CanApplyTo(ResultTarget target) => throw new NotImplementedException();

    /// <summary>指定された受け手とコールバックの待機の組み合わせを許可するかを判定します。</summary>
    public bool SupportsAwait(bool awaitReceiver, bool awaitCallbacks) => throw new NotImplementedException();
}
