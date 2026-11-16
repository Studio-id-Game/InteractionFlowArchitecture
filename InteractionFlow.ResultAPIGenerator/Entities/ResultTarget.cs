using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>Self と Other の方向付きの組を表します。</summary>
internal sealed class ResultTarget(
    ResultTypePattern self,
    ResultTypePattern other,
    bool appliesToSelfOnlyTemplates,
    ImmutableArray<string> successArguments = default,
    ImmutableArray<string> failureArguments = default)
{
    public ResultTypePattern Self { get; } = self;
    public ResultTypePattern Other { get; } = other;

    /// <summary>Self だけを使うテンプレートに適用できるかを表します。</summary>
    public bool AppliesToSelfOnlyTemplates { get; } = appliesToSelfOnlyTemplates;

    /// <summary>成功時の引数名を宣言順に保持します。default は未指定です。</summary>
    public ImmutableArray<string> SuccessArguments { get; } = successArguments;

    /// <summary>失敗時の引数名を宣言順に保持します。default は未指定です。</summary>
    public ImmutableArray<string> FailureArguments { get; } = failureArguments;

    /// <summary>Self、Other の順に型引数名を取り出し、共有名をまとめます。</summary>
    public ImmutableArray<string> GetTypeParameterNames() => throw new NotImplementedException();

    /// <summary>分岐の引数名を順に照合します。一意に解決できない位置は null です。</summary>
    /// <returns>コールバック引数名の一覧が未指定の場合は default。</returns>
    public ImmutableArray<IPropertySymbol?> MatchCallbackArguments(
        bool forSuccess,
        ImmutableArray<IPropertySymbol> availableProperties)
        => throw new NotImplementedException();

    /// <summary>Other の適合するファクトリと、引数順に対応する Self のプロパティを列挙します。</summary>
    public ImmutableArray<(IMethodSymbol Factory, ImmutableArray<IPropertySymbol> Arguments)> FindFactoryMatches(
        IMethodSymbol templateFactory,
        IReadOnlyDictionary<string, ITypeSymbol> typeArguments,
        ImmutableArray<(IPropertySymbol Property, ITypeSymbol Type)> sourceProperties,
        Compilation compilation,
        INamedTypeSymbol destination)
        => throw new NotImplementedException();
}
