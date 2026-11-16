using System;
using System.Linq;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace InteractionFlow.ResultAPIGenerator.Interactions.Rules;

internal static class GeneratedTypeShortener
{
    private const string TasksAlias = "Tasks";
    private const string QualifiedTasksNamespace = "global::System.Threading.Tasks";
    private static readonly UsingDirectiveSyntax TasksUsing =
        SyntaxFactory.ParseCompilationUnit($"using {TasksAlias} = {QualifiedTasksNamespace};").Usings[0]
            .WithTrailingTrivia(SyntaxFactory.LineFeed, SyntaxFactory.LineFeed);

    public static GeneratedSource Shorten(GeneratedSource source, Compilation compilation, CancellationToken cancellationToken)
    {
        var options = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions;
        var tree = CSharpSyntaxTree.ParseText(source.Source, options, source.HintName, cancellationToken: cancellationToken);
        var root = (CompilationUnitSyntax)tree.GetRoot(cancellationToken);
        var withAlias = source.Source.IndexOf(QualifiedTasksNamespace + ".", StringComparison.Ordinal) >= 0
            ? TryShortenWithTasksAlias(root, compilation, cancellationToken)
            : null;
        var shortened = withAlias ?? Rewrite(root, compilation.AddSyntaxTrees(tree).GetSemanticModel(tree), cancellationToken).Root;
        return shortened == root ? source : new GeneratedSource(source.HintName, shortened.ToFullString());
    }

    private static CompilationUnitSyntax? TryShortenWithTasksAlias(
        CompilationUnitSyntax root, Compilation compilation, CancellationToken cancellationToken)
    {
        // Adding the alias must leave existing unqualified references to Tasks alone.
        if (root.DescendantNodes().OfType<SimpleNameSyntax>().Any(IsUnqualifiedTasksName))
            return null;

        var alias = TasksUsing.WithLeadingTrivia(root.GetLeadingTrivia());
        var withAlias = root.WithLeadingTrivia(default(SyntaxTriviaList)).AddUsings(alias);
        var tree = CSharpSyntaxTree.Create(withAlias, (CSharpParseOptions)root.SyntaxTree.Options, root.SyntaxTree.FilePath);
        var model = compilation.AddSyntaxTrees(tree).GetSemanticModel(tree);
        var trial = (CompilationUnitSyntax)tree.GetRoot(cancellationToken);
        var directive = trial.Usings[trial.Usings.Count - 1];
        if (model.GetDiagnostics(directive.Span, cancellationToken).Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            return null;

        var (shortened, usedAlias) = Rewrite(trial, model, cancellationToken, useTasksAlias: true);
        return usedAlias ? shortened : null;
    }

    private static bool IsUnqualifiedTasksName(SimpleNameSyntax name) =>
        name.Identifier.ValueText == TasksAlias && name.Parent switch
        {
            QualifiedNameSyntax qualified => name != qualified.Right,
            AliasQualifiedNameSyntax => false,
            MemberAccessExpressionSyntax access => name != access.Name,
            _ => true,
        };

    private static (CompilationUnitSyntax Root, bool UsedTasksAlias) Rewrite(
        CompilationUnitSyntax root, SemanticModel model, CancellationToken cancellationToken, bool useTasksAlias = false)
    {
        var namespaceName = (root.Members.FirstOrDefault() as FileScopedNamespaceDeclarationSyntax)?.Name.ToString();
        var rewriter = new Rewriter(model, namespaceName, cancellationToken, useTasksAlias);
        return ((CompilationUnitSyntax)rewriter.Visit(root)!, rewriter.UsedTasksAlias);
    }

    private sealed class Rewriter(
        SemanticModel model, string? namespaceName, CancellationToken cancellationToken, bool useTasksAlias)
        : CSharpSyntaxRewriter
    {
        private readonly string qualifiedNamespace = "global::" + namespaceName;

        public bool UsedTasksAlias { get; private set; }

        public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
        {
            var qualifier = node.Left.ToString();
            if (qualifier == qualifiedNamespace
                && BindsToSameType(node, node.Right.ToString(), SpeculativeBindingOption.BindAsTypeOrNamespace))
            {
                return Visit(node.Right)!.WithTriviaFrom(node);
            }

            if (useTasksAlias && qualifier == QualifiedTasksNamespace
                && BindsToSameType(node, $"{TasksAlias}.{node.Right}", SpeculativeBindingOption.BindAsTypeOrNamespace))
            {
                UsedTasksAlias = true;
                return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName(TasksAlias), (SimpleNameSyntax)Visit(node.Right)!)
                    .WithTriviaFrom(node);
            }

            return base.VisitQualifiedName(node);
        }

        public override SyntaxNode? VisitAliasQualifiedName(AliasQualifiedNameSyntax node) =>
            namespaceName is null && node.Alias.Identifier.Text == "global"
            && BindsToSameType(node, node.Name.ToString(), node.Parent is MemberAccessExpressionSyntax
                ? SpeculativeBindingOption.BindAsExpression
                : SpeculativeBindingOption.BindAsTypeOrNamespace)
                ? Visit(node.Name)!.WithTriviaFrom(node)
                : base.VisitAliasQualifiedName(node);

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node) =>
            node.Expression.ToString() == qualifiedNamespace
            && BindsToSameType(node, node.Name.ToString(), SpeculativeBindingOption.BindAsExpression)
                ? Visit(node.Name)!.WithTriviaFrom(node)
                : base.VisitMemberAccessExpression(node);

        private bool BindsToSameType(ExpressionSyntax qualified, string replacementName, SpeculativeBindingOption binding)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var original = model.GetSymbolInfo(qualified, cancellationToken).Symbol;
            if (original is not INamedTypeSymbol { TypeKind: not TypeKind.Error, ContainingType: null })
                return false;

            // Bind a detached name without the original qualified-name parent.
            var name = SyntaxFactory.ParseName(replacementName);
            var replacement = model.GetSpeculativeSymbolInfo(qualified.SpanStart, name, binding).Symbol;
            return SymbolEqualityComparer.IncludeNullability.Equals(original, replacement);
        }
    }
}
