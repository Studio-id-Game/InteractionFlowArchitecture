using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using InteractionFlow.ResultAPIGenerator.Interactions.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed class DetectionInteraction : IDetectionInteraction
{
    public InteractionResult<DetectedExtension?> Detect(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var attribute = FindExtensionAttribute(context);
        return attribute is null
            ? new InteractionResult<DetectedExtension?>(null)
            : ResolveSymbols(context, attribute, cancellationToken);
    }

    private static AttributeData? FindExtensionAttribute(GeneratorAttributeSyntaxContext context)
    {
        var attribute = ResultApiAttributes.Find(context.TargetSymbol, ResultApiAttributes.Extension);
        var syntax = attribute?.ApplicationSyntaxReference;
        return syntax is not null && syntax.SyntaxTree == context.TargetNode.SyntaxTree
            && context.TargetNode.Span.Contains(syntax.Span) ? attribute : null;
    }

    private static InteractionResult<DetectedExtension?> ResolveSymbols(
        GeneratorAttributeSyntaxContext context,
        AttributeData extensionAttribute,
        CancellationToken cancellationToken)
    {
        var declaration = (ClassDeclarationSyntax)context.TargetNode;
        var destination = (INamedTypeSymbol)context.TargetSymbol;
        var arguments = extensionAttribute.ConstructorArguments;
        if (!destination.IsStatic || destination.IsFileLocal || destination.ContainingType is not null || destination.Arity != 0
            || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return Invalid("The Result API destination must be a non-generic top-level static partial class.");
        }

        if (arguments.Length is not (1 or 4) || GetType(arguments[0]) is not { } template
            || template.IsUnboundGenericType || template.DeclaringSyntaxReferences.IsEmpty
            || !ResultApiAttributes.GetTemplateOwners(template).Any())
        {
            return Invalid("The Result API template must be a marked source type in this compilation.");
        }

        var compilation = context.SemanticModel.Compilation;
        if (!SymbolEqualityComparer.Default.Equals(template.ContainingAssembly, compilation.Assembly))
            return Invalid("The Result API template must belong to this compilation.");

        var @namespace = destination.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : destination.ContainingNamespace.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
        var extensionClass = new ExtensionClass(@namespace, declaration.Identifier.Text, destination.Name);
        AwaitableTypes? awaitables = null;
        var suffix = string.Empty;
        if (arguments.Length == 4)
        {
            var withResult = GetType(arguments[1]);
            var withoutResult = GetType(arguments[2]);
            if (withResult is null || withoutResult is null || arguments[3].Value is not string asyncSuffix)
                return Invalid("Specify the result-bearing type, the resultless type, and a method suffix.");

            awaitables = AwaitableTypeRules.Resolve(compilation, destination, extensionClass,
                withResult, withoutResult, (CSharpParseOptions)declaration.SyntaxTree.Options, cancellationToken);
            if (awaitables is null)
            {
                return Invalid("Specify an open generic async return type that awaits to its type argument "
                    + "and a resultless async return type that awaits to void.");
            }

            suffix = asyncSuffix;
        }

        var definition = new ExtensionDefinition(extensionClass, template, awaitables, suffix);
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var targets = ReadTargets(template, destination, diagnostics, cancellationToken);
        return new InteractionResult<DetectedExtension?>(
            new DetectedExtension(definition, destination, targets), diagnostics.ToImmutable());

        InteractionResult<DetectedExtension?> Invalid(string message) =>
            new(null, ImmutableArray.Create(GeneratorDiagnostics.Create(declaration.GetLocation(), message)));
    }

    private static INamedTypeSymbol? GetType(TypedConstant constant) =>
        constant.Kind == TypedConstantKind.Type && constant.Value is INamedTypeSymbol { TypeKind: not TypeKind.Error } type
            ? type
            : null;

    private static ImmutableArray<ResultTarget> ReadTargets(
        INamedTypeSymbol template,
        INamedTypeSymbol destination,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        var patterns = ImmutableArray.CreateBuilder<ResultTarget>();
        for (INamedTypeSymbol? type = template; type is not null; type = type.BaseType)
            ReadAttributes(type);
        ReadAttributes(destination);

        return patterns.ToImmutable();

        void ReadAttributes(INamedTypeSymbol type)
        {
            foreach (var attribute in type.GetAttributes())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!ResultApiAttributes.Is(attribute, ResultApiAttributes.Target))
                    continue;

                var values = attribute.ConstructorArguments;
                var same = values.Length == 2;
                if (values.Length is not (2 or 5) || ReadPattern(values[0], values[1]) is not { } self)
                {
                    Report("Invalid Self type or type parameter names.");
                    continue;
                }

                var other = same ? self : ReadPattern(values[2], values[3]);
                if (other is null)
                {
                    Report("Invalid Other type or type parameter names.");
                    continue;
                }

                patterns.Add(new ResultTarget(self, other, same));
                if (!same && values[4].Value is true)
                    patterns.Add(new ResultTarget(other, self, false));

                void Report(string message)
                {
                    var location = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation();
                    diagnostics.Add(GeneratorDiagnostics.Create(location, message));
                }
            }
        }
    }

    private static ResultTypePattern? ReadPattern(TypedConstant typeArgument, TypedConstant namesArgument)
    {
        if (GetType(typeArgument) is not { } type || type.ContainingType is { IsGenericType: true })
            return null;

        if (namesArgument.IsNull)
        {
            return !type.IsUnboundGenericType && (type.Arity == 0
                || !SymbolEqualityComparer.Default.Equals(type, type.ConstructedFrom))
                ? new ResultTypePattern(type, ImmutableArray<string>.Empty)
                : null;
        }

        if (namesArgument.Value is not string names || type.Arity == 0 || type.TypeParameters.Any(TypeNames.HasConstraints))
            return null;

        var parameters = names.Split(',');
        if (parameters.Length != type.Arity)
            return null;

        var parsed = ImmutableArray.CreateBuilder<string>(parameters.Length);
        foreach (var parameter in parameters)
        {
            if (!TypeNames.TryParseIdentifier(parameter.Trim(), out var name))
                return null;

            parsed.Add(name);
        }

        return new ResultTypePattern(type, parsed.MoveToImmutable());
    }
}
