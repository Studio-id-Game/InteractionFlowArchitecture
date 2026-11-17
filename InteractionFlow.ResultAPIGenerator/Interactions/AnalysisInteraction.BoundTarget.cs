using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using InteractionFlow.ResultAPIGenerator.Entities;
using InteractionFlow.ResultAPIGenerator.Interactions.Rules;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed partial class AnalysisInteraction
{
    private static List<BoundTarget> BindTargets(AnalysisContext context)
    {
        var targets = new List<BoundTarget>();
        foreach (var target in context.Extension.Targets)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (BindResultTypes(target, context) is not { } bound)
                continue;

            var index = targets.FindIndex(bound.HasSameTypes);
            if (index < 0)
                targets.Add(bound);
            else if (target.AppliesToSelfOnlyTemplates && !targets[index].Target.AppliesToSelfOnlyTemplates)
                targets[index] = bound;
        }

        return targets;
    }

    private static BoundTarget? BindResultTypes(ResultTarget target, AnalysisContext context)
    {
        var names = target.TypeParameterNames;
        // Use distinct unconstrained symbols from this compilation for generated type variables.
        var variables = context.TypeVariables;
        if (!names.IsEmpty && (variables.IsDefault || names.Length > variables.Length))
            return null;

        var typeArguments = new Dictionary<string, ITypeSymbol>(names.Length, StringComparer.Ordinal);
        for (var index = 0; index < names.Length; index++)
            typeArguments.Add(names[index], variables[index]);

        return target.Self.Bind(typeArguments) is { } self && target.Other.Bind(typeArguments) is { } other
            ? new BoundTarget(target, self, other, variables, context)
            : null;
    }

    private sealed class BoundTarget(
        ResultTarget target,
        INamedTypeSymbol self,
        INamedTypeSymbol other,
        ImmutableArray<ITypeParameterSymbol> typeVariables,
        AnalysisContext context)
    {
        private readonly (ImmutableArray<IPropertySymbol> Success, ImmutableArray<IPropertySymbol> Failure) properties =
            ReadProperties(context, self);
        private readonly AnalysisContext analysisContext = context;
        private readonly bool guaranteesNonNullBranchValues =
            self.ContainingNamespace.ToDisplayString() == "InteractionFlow.Core.ResultAPI.Entities"
            && self.Name == "Result" && self.Arity <= 2;

        public ResultTarget Target { get; } = target;
        public INamedTypeSymbol Self { get; } = self;
        public INamedTypeSymbol Other { get; } = other;
        public ImmutableArray<ITypeParameterSymbol> TypeVariables { get; } = typeVariables;
        public bool HasSuccessCheck { get; } = HasSuccessProperty(context, self);
        public bool HasSameResultType { get; } = SymbolEqualityComparer.IncludeNullability.Equals(self, other);

        public bool HasSameTypes(BoundTarget otherTarget) =>
            SymbolEqualityComparer.IncludeNullability.Equals(Self, otherTarget.Self)
            && SymbolEqualityComparer.IncludeNullability.Equals(Other, otherTarget.Other);

        public ImmutableArray<IPropertySymbol> GetProperties(bool forSuccess) =>
            forSuccess ? properties.Success : properties.Failure;

        public ITypeSymbol GetPropertyType(IPropertySymbol property) =>
            property.Type.WithNullableAnnotation(
                guaranteesNonNullBranchValues && property.NullableAnnotation == NullableAnnotation.Annotated
                    ? NullableAnnotation.NotAnnotated
                    : property.NullableAnnotation);

        public ImmutableArray<(IMethodSymbol Factory, ImmutableArray<IPropertySymbol> Arguments)> FindFactories(string name)
        {
            var properties = new Dictionary<string, IPropertySymbol?>(StringComparer.Ordinal);
            foreach (var property in GetProperties(name == "CreateSuccess"))
                properties[property.Name] = properties.ContainsKey(property.Name) ? null : property;

            var matches = ImmutableArray.CreateBuilder<(IMethodSymbol, ImmutableArray<IPropertySymbol>)>();
            foreach (var member in Other.GetMembers(name))
            {
                if (member is not IMethodSymbol { IsStatic: true, MethodKind: MethodKind.Ordinary, Arity: 0 } factory
                    || !SymbolEqualityComparer.Default.Equals(factory.ReturnType, Other)
                    || (factory.ReturnNullableAnnotation == NullableAnnotation.Annotated
                        && Other.NullableAnnotation != NullableAnnotation.Annotated)
                    || !analysisContext.Compilation.IsSymbolAccessibleWithin(factory, analysisContext.Extension.DestinationSymbol))
                {
                    continue;
                }

                var arguments = MatchFactoryArguments(factory, properties);
                if (!arguments.IsDefault)
                    matches.Add((factory, arguments));
            }

            return matches.ToImmutable();
        }

        private ImmutableArray<IPropertySymbol> MatchFactoryArguments(
            IMethodSymbol factory, IReadOnlyDictionary<string, IPropertySymbol?> properties)
        {
            var arguments = ImmutableArray.CreateBuilder<IPropertySymbol>(factory.Parameters.Length);
            foreach (var parameter in factory.Parameters)
            {
                if (parameter.RefKind != RefKind.None
                    || !properties.TryGetValue(parameter.Name, out var property)
                    || property is null)
                {
                    return default;
                }

                var sourceType = GetPropertyType(property);
                if (sourceType.NullableAnnotation == NullableAnnotation.Annotated
                    && parameter.NullableAnnotation == NullableAnnotation.NotAnnotated)
                {
                    return default;
                }

                var conversion = analysisContext.Compilation.ClassifyCommonConversion(sourceType, parameter.Type);
                if (!conversion.IsIdentity && !(conversion.IsImplicit && conversion.IsReference))
                    return default;

                arguments.Add(property);
            }

            return arguments.MoveToImmutable();
        }

        private static bool HasSuccessProperty(AnalysisContext context, INamedTypeSymbol self)
        {
            for (INamedTypeSymbol? type = self; type is not null; type = type.BaseType)
            {
                var members = type.GetMembers("IsSuccess");
                if (!members.IsEmpty)
                {
                    return members.Length == 1 && members[0] is IPropertySymbol { Type.SpecialType: SpecialType.System_Boolean } property
                        && CanReadProperty(context, self, property);
                }
            }

            return false;
        }

        private static (ImmutableArray<IPropertySymbol> Success, ImmutableArray<IPropertySymbol> Failure)
            ReadProperties(AnalysisContext context, INamedTypeSymbol self)
        {
            var successProperties = new List<(IPropertySymbol Property, int? Index)>();
            var failureProperties = new List<(IPropertySymbol Property, int? Index)>();
            var shadowedNames = new HashSet<string>(StringComparer.Ordinal);
            for (INamedTypeSymbol? type = self; type is not null; type = type.BaseType)
            {
                var members = type.GetMembers();
                foreach (var property in members.OfType<IPropertySymbol>())
                {
                    if (!shadowedNames.Contains(property.Name)
                        && CanReadProperty(context, self, property)
                        && ResultApiAttributes.Find(property, ResultApiAttributes.CallbackArgument) is { } attribute
                        && attribute.ConstructorArguments.Length is 1 or 2
                        && attribute.ConstructorArguments[0].Value is bool success)
                    {
                        var arguments = attribute.ConstructorArguments;
                        int? index = arguments.Length == 2 && arguments[1].Value is int value ? value : null;
                        (success ? successProperties : failureProperties).Add((property, index));
                    }
                }
                foreach (var member in members)
                    shadowedNames.Add(member.Name);
            }

            return (OrderArguments(successProperties), OrderArguments(failureProperties));

            static ImmutableArray<IPropertySymbol> OrderArguments(List<(IPropertySymbol Property, int? Index)> properties) =>
                properties.OrderBy(static item => item.Index is null)
                    .ThenBy(static item => item.Index)
                    .ThenBy(static item => item.Property.Name, StringComparer.Ordinal)
                    .Select(static item => item.Property)
                    .ToImmutableArray();
        }

        private static bool CanReadProperty(AnalysisContext context, INamedTypeSymbol self, IPropertySymbol property) =>
            property is { IsStatic: false, IsIndexer: false, GetMethod: { } getter }
            && context.Compilation.IsSymbolAccessibleWithin(getter, context.Extension.DestinationSymbol, self);
    }
}
