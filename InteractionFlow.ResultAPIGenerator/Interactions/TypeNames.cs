using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal static class TypeNames
{
    private static readonly SymbolDisplayFormat SourceFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly SymbolDisplayFormat KeyFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.ExpandNullable
            | SymbolDisplayMiscellaneousOptions.ExpandValueTuple);

    public static string Format(
        ITypeSymbol type,
        IReadOnlyDictionary<ITypeParameterSymbol, string> names,
        bool forSignature = false)
    {
        var parts = type.ToDisplayParts(forSignature ? KeyFormat : SourceFormat);
        return string.Concat(parts.Select(part =>
        {
            if (part.Symbol is ITypeParameterSymbol parameter && names.TryGetValue(parameter, out var name))
                return Escape(name);
            if (forSignature && part.Kind == SymbolDisplayPartKind.Keyword && part.ToString() == "dynamic")
                return "object";
            return part.ToString();
        }));
    }

    public static string Escape(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
            ? "@" + name
            : name;

    public static bool HasConstraints(ITypeParameterSymbol parameter) =>
        parameter.ConstraintTypes.Length != 0 || parameter.HasReferenceTypeConstraint
        || parameter.HasValueTypeConstraint || parameter.HasNotNullConstraint
        || parameter.HasUnmanagedTypeConstraint || parameter.HasConstructorConstraint;

    public static bool IsIdentifier(string name)
    {
        var token = SyntaxFactory.ParseToken(Escape(name));
        return token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == name && !token.ContainsDiagnostics;
    }

    public static string ReplaceTypeParameters(string text, IReadOnlyDictionary<string, string> substitutions, bool isBody = false)
    {
        if (substitutions.Count == 0)
            return text;

        var syntax = isBody ? (CSharpSyntaxNode)SyntaxFactory.ParseStatement(text) : SyntaxFactory.ParseTypeName(text);
        return new TypeParameterRewriter(substitutions).Visit(syntax)!.ToFullString();
    }

    private sealed class TypeParameterRewriter(IReadOnlyDictionary<string, string> substitutions) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (node.Parent is not (QualifiedNameSyntax or AliasQualifiedNameSyntax)
                && !(node.Parent is MemberAccessExpressionSyntax access && access.Name == node)
                && substitutions.TryGetValue(node.Identifier.ValueText, out var replacement))
            {
                return SyntaxFactory.IdentifierName(replacement).WithTriviaFrom(node);
            }

            return base.VisitIdentifierName(node);
        }
    }
}
