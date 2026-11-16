using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>テンプレートメソッドと、候補の選択・展開に必要な条件を表します。</summary>
internal sealed class TemplateMethod(
    IMethodSymbol method,
    bool usesOther,
    bool allowAwaitedReceiver,
    bool allowAwaitedCallbacks)
{
    public IMethodSymbol Method { get; } = method;
    public bool AllowAwaitedReceiver { get; } = allowAwaitedReceiver;
    public bool AllowAwaitedCallbacks { get; } = allowAwaitedCallbacks;

    /// <summary>Self と Other の参照に基づいて、対象の組を展開候補にできるかを判定します。</summary>
    public bool CanApplyTo(ResultTarget target) => usesOther || target.AppliesToSelfOnlyTemplates;
}
