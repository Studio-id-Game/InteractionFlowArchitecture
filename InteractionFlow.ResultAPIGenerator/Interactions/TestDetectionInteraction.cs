using System.Linq;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed class TestDetectionInteraction : IDetectionInteraction
{
    private static readonly SymbolDisplayFormat NamespaceFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    public InteractionResult<DetectedExtension?> Detect(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var type = (INamedTypeSymbol)context.TargetSymbol;
        var declaration = (ClassDeclarationSyntax)context.TargetNode;
        var arguments = context.Attributes[0].ConstructorArguments;
        if (!type.IsStatic || type.IsFileLocal || type.ContainingType is not null
            || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword)
            || arguments.IsDefaultOrEmpty
            || arguments[0].Kind != TypedConstantKind.Type
            || arguments[0].Value is not INamedTypeSymbol template)
        {
            return new InteractionResult<DetectedExtension?>(null);
        }

        var namespaceName = type.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : type.ContainingNamespace.ToDisplayString(NamespaceFormat);
        var declarationName = declaration.Identifier.Text;
        var hintName = type.Name;
        if (declaration.TypeParameterList is { } typeParameters)
        {
            var names = typeParameters.Parameters.Select(static parameter => parameter.Identifier.Text);
            declarationName += $"<{string.Join(", ", names)}>";
            hintName += $".{type.Arity}";
        }

        var destination = new ExtensionClass(namespaceName, declarationName, hintName);
        var definition = new ExtensionDefinition(destination, template, awaitables: null, suffix: string.Empty);
        return new InteractionResult<DetectedExtension?>(new DetectedExtension(definition, type, default));
    }
}
