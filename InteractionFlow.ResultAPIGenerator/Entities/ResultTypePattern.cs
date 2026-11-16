using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>Result 型の定義と、宣言で指定された型引数名を表します。</summary>
internal sealed class ResultTypePattern(INamedTypeSymbol typeDefinition, ImmutableArray<string> typeArgumentNames)
{
    public INamedTypeSymbol TypeDefinition { get; } = typeDefinition;
    public ImmutableArray<string> TypeArgumentNames { get; } =
        typeArgumentNames.IsDefault ? ImmutableArray<string>.Empty : typeArgumentNames;

    /// <summary>名前で型引数を束縛します。すべて束縛できない場合は null を返します。</summary>
    public INamedTypeSymbol? Bind(IReadOnlyDictionary<string, ITypeSymbol> typeArguments)
    {
        if (TypeDefinition.TypeKind == TypeKind.Error)
            return null;

        var definition = TypeDefinition.IsUnboundGenericType
            ? TypeDefinition.OriginalDefinition
            : TypeDefinition.ConstructedFrom;
        if (TypeArgumentNames.IsDefaultOrEmpty)
        {
            return definition.Arity == 0 || (!TypeDefinition.IsUnboundGenericType
                && !SymbolEqualityComparer.Default.Equals(TypeDefinition, definition))
                ? TypeDefinition
                : null;
        }

        if (TypeArgumentNames.Length != definition.Arity)
            return null;

        var arguments = ImmutableArray.CreateBuilder<ITypeSymbol>(definition.Arity);
        var annotations = ImmutableArray.CreateBuilder<NullableAnnotation>(definition.Arity);
        foreach (var name in TypeArgumentNames)
        {
            if (!typeArguments.TryGetValue(name, out var argument) || argument.TypeKind == TypeKind.Error)
                return null;

            arguments.Add(argument);
            annotations.Add(argument.NullableAnnotation);
        }

        return definition.Construct(arguments.MoveToImmutable(), annotations.MoveToImmutable());
    }
}
