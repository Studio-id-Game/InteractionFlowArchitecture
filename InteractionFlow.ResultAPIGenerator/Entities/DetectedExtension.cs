using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>同じコンパイル内で検出から分析へ渡す生成先の宣言です。</summary>
internal sealed class DetectedExtension(
    ExtensionDefinition definition,
    INamedTypeSymbol destinationSymbol,
    ImmutableArray<ResultTarget> targets)
{
    public ExtensionDefinition Definition { get; } = definition;
    public INamedTypeSymbol DestinationSymbol { get; } = destinationSymbol;
    public ImmutableArray<ResultTarget> Targets { get; } =
        targets.IsDefault ? ImmutableArray<ResultTarget>.Empty : targets;
}
