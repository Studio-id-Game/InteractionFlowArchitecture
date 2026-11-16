using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>結果値ありと結果値なしの待機可能型を表します。</summary>
internal sealed class AwaitableTypes(INamedTypeSymbol withResult, INamedTypeSymbol withoutResult)
{
    private readonly INamedTypeSymbol resultDefinition =
        withResult.IsUnboundGenericType ? withResult.OriginalDefinition : withResult.ConstructedFrom;

    /// <summary>戻り値型を待機可能型に包みます。戻り値型の指定がない場合と void には、結果値なしの型を使います。</summary>
    public INamedTypeSymbol Wrap(ITypeSymbol? returnType)
    {
        if (returnType is null || returnType.SpecialType == SpecialType.System_Void)
            return withoutResult;

        return resultDefinition.Construct(ImmutableArray.Create(returnType), ImmutableArray.Create(returnType.NullableAnnotation));
    }
}
