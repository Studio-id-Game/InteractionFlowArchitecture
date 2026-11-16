using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>Result 型の定義と、宣言で指定された型引数名を表します。</summary>
internal sealed class ResultTypePattern(INamedTypeSymbol typeDefinition, ImmutableArray<string> typeArgumentNames)
{
    public INamedTypeSymbol TypeDefinition { get; } = typeDefinition;
    public ImmutableArray<string> TypeArgumentNames { get; } = typeArgumentNames;

    /// <summary>名前で型引数を束縛します。すべて束縛できない場合は null を返します。</summary>
    public INamedTypeSymbol? Bind(IReadOnlyDictionary<string, ITypeSymbol> typeArguments)
        => throw new NotImplementedException();
}
