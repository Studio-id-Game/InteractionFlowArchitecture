using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>Interaction の処理結果と診断を保持します。</summary>
internal readonly struct InteractionResult<T>(T value, ImmutableArray<Diagnostic> diagnostics = default)
{
    public T Value { get; } = value;

    public ImmutableArray<Diagnostic> Diagnostics =>
        diagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : diagnostics;
}
