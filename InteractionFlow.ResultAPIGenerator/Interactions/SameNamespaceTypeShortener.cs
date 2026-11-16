using System.Linq;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal static class SameNamespaceTypeShortener
{
    public static GeneratedSource Shorten(GeneratedSource source, Compilation compilation, CancellationToken cancellationToken)
    {
        var options = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions;
        var tree = CSharpSyntaxTree.ParseText(source.Source, options, source.HintName, cancellationToken: cancellationToken);
        var root = (CompilationUnitSyntax)tree.GetRoot(cancellationToken);
        var namespaceName = (root.Members.FirstOrDefault() as FileScopedNamespaceDeclarationSyntax)?.Name.ToString();
        var model = compilation.AddSyntaxTrees(tree).GetSemanticModel(tree);
        var shortened = new Rewriter(model, namespaceName, cancellationToken).Visit(root);
        return shortened == root ? source : new GeneratedSource(source.HintName, shortened!.ToFullString());
    }

    private sealed class Rewriter(SemanticModel model, string? namespaceName, CancellationToken cancellationToken)
        : CSharpSyntaxRewriter
    {
        private readonly string qualifiedNamespace = "global::" + namespaceName;

        public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node) =>
            node.Left.ToString() == qualifiedNamespace
            && BindsToSameType(node, node.Right, SpeculativeBindingOption.BindAsTypeOrNamespace)
                ? Visit(node.Right)!.WithTriviaFrom(node)
                : base.VisitQualifiedName(node);

        public override SyntaxNode? VisitAliasQualifiedName(AliasQualifiedNameSyntax node) =>
            namespaceName is null && node.Alias.Identifier.Text == "global"
            && BindsToSameType(node, node.Name, node.Parent is MemberAccessExpressionSyntax
                ? SpeculativeBindingOption.BindAsExpression
                : SpeculativeBindingOption.BindAsTypeOrNamespace)
                ? Visit(node.Name)!.WithTriviaFrom(node)
                : base.VisitAliasQualifiedName(node);

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node) =>
            node.Expression.ToString() == qualifiedNamespace
            && BindsToSameType(node, node.Name, SpeculativeBindingOption.BindAsExpression)
                ? Visit(node.Name)!.WithTriviaFrom(node)
                : base.VisitMemberAccessExpression(node);

        private bool BindsToSameType(ExpressionSyntax qualified, SimpleNameSyntax simple, SpeculativeBindingOption binding)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var original = model.GetSymbolInfo(qualified, cancellationToken).Symbol;
            if (original is not INamedTypeSymbol { TypeKind: not TypeKind.Error, ContainingType: null })
                return false;

            // Bind the simple name without its original qualified-name parent.
            var unqualified = SyntaxFactory.ParseName(simple.ToString());
            var replacement = model.GetSpeculativeSymbolInfo(qualified.SpanStart, unqualified, binding).Symbol;
            return SymbolEqualityComparer.IncludeNullability.Equals(original, replacement);
        }
    }
}
