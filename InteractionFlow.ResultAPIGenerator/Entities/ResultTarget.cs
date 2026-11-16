using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>Self と Other の方向付きの組を表します。</summary>
internal sealed class ResultTarget(
    ResultTypePattern self,
    ResultTypePattern other,
    bool appliesToSelfOnlyTemplates)
{
    public ResultTypePattern Self { get; } = self;
    public ResultTypePattern Other { get; } = other;

    /// <summary>Self だけを使うテンプレートに適用できるかを表します。</summary>
    public bool AppliesToSelfOnlyTemplates { get; } = appliesToSelfOnlyTemplates;

    /// <summary>Self、Other の順に型引数名を取り出し、共有名をまとめます。</summary>
    public ImmutableArray<string> GetTypeParameterNames()
    {
        var names = ImmutableArray.CreateBuilder<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        AddNames(Self.TypeArgumentNames);
        AddNames(Other.TypeArgumentNames);
        return names.ToImmutable();

        void AddNames(ImmutableArray<string> typeArgumentNames)
        {
            if (typeArgumentNames.IsDefaultOrEmpty)
                return;

            foreach (var name in typeArgumentNames)
            {
                if (seen.Add(name))
                    names.Add(name);
            }
        }
    }

    /// <summary>Other の適合するファクトリと、引数順に対応する Self のプロパティを列挙します。</summary>
    public static ImmutableArray<(IMethodSymbol Factory, ImmutableArray<IPropertySymbol> Arguments)> FindFactoryMatches(
        IMethodSymbol templateFactory,
        INamedTypeSymbol selfType,
        INamedTypeSymbol otherType,
        ImmutableArray<(IPropertySymbol Property, ITypeSymbol Type)> sourceProperties,
        Compilation compilation,
        INamedTypeSymbol destination)
    {
        var matches = ImmutableArray.CreateBuilder<(IMethodSymbol Factory, ImmutableArray<IPropertySymbol> Arguments)>();
        var properties = new Dictionary<string, (IPropertySymbol Property, ITypeSymbol Type)?>(StringComparer.Ordinal);
        if (!sourceProperties.IsDefaultOrEmpty)
        {
            foreach (var source in sourceProperties)
            {
                var property = source.Property;
                if (IsReadableInstanceProperty(property) && property.GetMethod is { } getter
                    && compilation.IsSymbolAccessibleWithin(getter, destination, selfType))
                {
                    properties[property.Name] = properties.ContainsKey(property.Name) ? null : source;
                }
            }
        }

        foreach (var member in otherType.GetMembers(templateFactory.Name))
        {
            if (member is not IMethodSymbol { IsStatic: true, MethodKind: MethodKind.Ordinary, Arity: 0 } factory
                || !SymbolEqualityComparer.Default.Equals(factory.ReturnType, otherType)
                || (factory.ReturnNullableAnnotation == NullableAnnotation.Annotated
                    && otherType.NullableAnnotation != NullableAnnotation.Annotated)
                || !compilation.IsSymbolAccessibleWithin(factory, destination))
            {
                continue;
            }

            var arguments = MatchFactoryArguments(factory, properties, compilation);
            if (!arguments.IsDefault)
                matches.Add((factory, arguments));
        }

        return matches.ToImmutable();
    }

    private static bool IsReadableInstanceProperty(IPropertySymbol property) =>
        !property.IsStatic && !property.IsIndexer && property.GetMethod is not null;

    private static ImmutableArray<IPropertySymbol> MatchFactoryArguments(
        IMethodSymbol factory,
        Dictionary<string, (IPropertySymbol Property, ITypeSymbol Type)?> properties,
        Compilation compilation)
    {
        var arguments = ImmutableArray.CreateBuilder<IPropertySymbol>(factory.Parameters.Length);
        foreach (var parameter in factory.Parameters)
        {
            if (parameter.RefKind != RefKind.None
                || !properties.TryGetValue(parameter.Name, out var property)
                || property is not { } source
                || (source.Type.NullableAnnotation == NullableAnnotation.Annotated
                    && parameter.NullableAnnotation == NullableAnnotation.NotAnnotated))
            {
                return default;
            }

            var conversion = compilation.ClassifyCommonConversion(source.Type, parameter.Type);
            if (!conversion.IsIdentity && !(conversion.IsImplicit && conversion.IsReference))
                return default;

            arguments.Add(source.Property);
        }

        return arguments.MoveToImmutable();
    }
}
