using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>結果値ありと結果値なしの待機可能型を表します。</summary>
internal sealed class AwaitableTypes(INamedTypeSymbol withResult, INamedTypeSymbol withoutResult)
{
    public INamedTypeSymbol WithResult { get; } = withResult;
    public INamedTypeSymbol WithoutResult { get; } = withoutResult;

    /// <summary>戻り値型を待機可能型に包みます。void には結果値なしの型を使います。</summary>
    public INamedTypeSymbol Wrap(ITypeSymbol returnType)
    {
        if (returnType.SpecialType == SpecialType.System_Void)
            return WithoutResult;

        var definition = WithResult.IsUnboundGenericType ? WithResult.OriginalDefinition : WithResult.ConstructedFrom;
        return definition.Construct(ImmutableArray.Create(returnType), ImmutableArray.Create(returnType.NullableAnnotation));
    }
}
