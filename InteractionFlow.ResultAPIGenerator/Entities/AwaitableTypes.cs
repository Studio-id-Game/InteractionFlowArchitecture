using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>結果値ありと結果値なしの待機可能型を表します。</summary>
internal sealed class AwaitableTypes(
    INamedTypeSymbol withResult,
    INamedTypeSymbol withoutResult,
    INamedTypeSymbol? configuredReceiver,
    bool configureWithResult,
    bool configureWithoutResult)
{
    private readonly INamedTypeSymbol resultDefinition = withResult.OriginalDefinition;
    private readonly INamedTypeSymbol? configuredDefinition = configuredReceiver?.OriginalDefinition;

    /// <summary>戻り値型を待機可能型に包みます。戻り値型の指定がない場合と void には、結果値なしの型を使います。</summary>
    public INamedTypeSymbol Wrap(ITypeSymbol? returnType)
    {
        if (GetResultType(returnType) is not { } result)
            return withoutResult;

        return Construct(resultDefinition, result);
    }

    public INamedTypeSymbol? WrapConfiguredReceiver(ITypeSymbol resultType) =>
        configuredDefinition is null ? null : Construct(configuredDefinition, resultType);

    public bool UsesConfigureAwait(ITypeSymbol? returnType) =>
        GetResultType(returnType) is null ? configureWithoutResult : configureWithResult;

    private static ITypeSymbol? GetResultType(ITypeSymbol? returnType) =>
        returnType?.SpecialType == SpecialType.System_Void ? null : returnType;

    private static INamedTypeSymbol Construct(INamedTypeSymbol definition, ITypeSymbol result) =>
        definition.Construct(ImmutableArray.Create(result), ImmutableArray.Create(result.NullableAnnotation));
}
