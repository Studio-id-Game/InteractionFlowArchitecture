using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace InteractionFlow.ResultAPIGenerator.Interactions.Rules;

internal static class AwaitableTypeRules
{
    private const string ProbeTypeParameter = "__ResultApiValue";

    public static (AwaitableTypes? Types, string? Error) Resolve(
        GeneratorAttributeSyntaxContext context,
        ExtensionClass declaration,
        INamedTypeSymbol withResult,
        INamedTypeSymbol withoutResult,
        INamedTypeSymbol? configuredReceiver,
        CancellationToken cancellationToken)
    {
        const string invalidReturnTypes = "Specify an open generic async return type that awaits to its type argument "
            + "and a resultless async return type that awaits to void.";
        const string invalidReceiver = "ConfiguredFuncType must be an open generic type that awaits to its type argument.";
        if (!withResult.IsUnboundGenericType || withResult.Arity != 1 || withoutResult.Arity != 0)
            return (null, invalidReturnTypes);
        if (configuredReceiver is not null)
        {
            if (!configuredReceiver.IsUnboundGenericType || configuredReceiver.Arity != 1)
                return (null, invalidReceiver);
            if (SymbolEqualityComparer.Default.Equals(configuredReceiver.OriginalDefinition, withResult.OriginalDefinition))
                return (null, "ConfiguredFuncType must be distinct from the regular awaitable receiver type.");
        }

        // Bind in the same namespace and partial class as the generated methods.
        var destination = (INamedTypeSymbol)context.TargetSymbol;
        var valueType = FormatOpenType(withResult);
        var voidType = withoutResult.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var prefix = "__CheckResultApiAwaitable";
        while (destination.Name.StartsWith(prefix, StringComparison.Ordinal)
            || destination.GetMembers().Any(member => member.Name.StartsWith(prefix, StringComparison.Ordinal)))
        {
            prefix += "_";
        }

        var namespaceDeclaration = declaration.Namespace.Length == 0 ? "" : $"namespace {declaration.Namespace};\n";
        var receiverProbe = configuredReceiver is null ? "" : $"""
            private static async {valueType} {prefix}Receiver<{ProbeTypeParameter}>({FormatOpenType(configuredReceiver)} value)
                => await value;
            """;
        var source = $$"""
            #nullable enable
            {{namespaceDeclaration}}static partial class {{declaration.DeclarationName}}
            {
                private static async {{valueType}} {{prefix}}Value<{{ProbeTypeParameter}}>({{valueType}} value)
                    => await value;
                private static async {{voidType}} {{prefix}}Void({{voidType}} value)
                    { await value; }
                private static async {{valueType}} {{prefix}}ConfiguredValue<{{ProbeTypeParameter}}>({{valueType}} value)
                    => {{FormatAwait("value", useConfigureAwait: true)}};
                private static async {{voidType}} {{prefix}}ConfiguredVoid({{voidType}} value)
                    { {{FormatAwait("value", useConfigureAwait: true)}}; }
                {{receiverProbe}}
            }
            """;
        var tree = CSharpSyntaxTree.ParseText(source, (CSharpParseOptions)context.TargetNode.SyntaxTree.Options,
            cancellationToken: cancellationToken);
        var model = context.SemanticModel.Compilation.AddSyntaxTrees(tree).GetSemanticModel(tree);
        var probes = tree.GetRoot(cancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>()
            .ToDictionary(static probe => probe.Identifier.ValueText, StringComparer.Ordinal);
        if (!IsSupported("Value", withValue: true) || !IsSupported("Void", withValue: false))
            return (null, invalidReturnTypes);
        if (configuredReceiver is not null && !IsSupported("Receiver", withValue: true))
            return (null, invalidReceiver);

        return (new AwaitableTypes(withResult, withoutResult, configuredReceiver,
            configureWithResult: IsSupported("ConfiguredValue", withValue: true),
            configureWithoutResult: IsSupported("ConfiguredVoid", withValue: false)), null);

        bool IsSupported(string name, bool withValue)
        {
            var probe = probes[prefix + name];
            if (model.GetDiagnostics(probe.Span, cancellationToken).Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
                return false;

            var expression = probe.DescendantNodes().OfType<AwaitExpressionSyntax>().Single();
            var resultType = model.GetAwaitExpressionInfo(expression).GetResultMethod?.ReturnType;
            if (!withValue)
                return resultType?.SpecialType == SpecialType.System_Void;

            var method = model.GetDeclaredSymbol(probe, cancellationToken);
            return resultType is not null && method is not null
                && SymbolEqualityComparer.Default.Equals(resultType, method.TypeParameters[0]);
        }
    }

    private static string FormatOpenType(INamedTypeSymbol type)
    {
        var definition = type.OriginalDefinition;
        var names = new Dictionary<ITypeParameterSymbol, string>(SymbolEqualityComparer.Default)
        {
            [definition.TypeParameters[0]] = ProbeTypeParameter,
        };
        return TypeNames.Format(definition, names);
    }

    public static string FormatAwait(string expression, bool useConfigureAwait) =>
        useConfigureAwait ? $"await {expression}.ConfigureAwait(false)" : $"await {expression}";
}
