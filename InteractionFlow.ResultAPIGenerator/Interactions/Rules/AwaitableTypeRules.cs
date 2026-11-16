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
    public static AwaitableTypes? Resolve(
        Compilation compilation,
        INamedTypeSymbol destination,
        ExtensionClass declaration,
        INamedTypeSymbol withResult,
        INamedTypeSymbol withoutResult,
        CSharpParseOptions parseOptions,
        CancellationToken cancellationToken)
    {
        if (!withResult.IsUnboundGenericType || withResult.Arity != 1 || withoutResult.Arity != 0)
            return null;

        // Bind in the same namespace and partial class as the generated methods.
        var generic = withResult.OriginalDefinition;
        const string typeParameter = "__ResultApiValue";
        var names = new Dictionary<ITypeParameterSymbol, string>(SymbolEqualityComparer.Default)
        {
            [generic.TypeParameters[0]] = typeParameter,
        };
        var valueType = TypeNames.Format(generic, names);
        var voidType = TypeNames.Format(withoutResult, names);
        var prefix = "__CheckResultApiAwaitable";
        while (destination.Name.StartsWith(prefix, StringComparison.Ordinal)
            || destination.GetMembers().Any(member => member.Name.StartsWith(prefix, StringComparison.Ordinal)))
        {
            prefix += "_";
        }

        var namespaceDeclaration = declaration.Namespace.Length == 0 ? "" : $"namespace {declaration.Namespace};\n";
        var source = $$"""
            #nullable enable
            {{namespaceDeclaration}}static partial class {{declaration.DeclarationName}}
            {
                private static async {{valueType}} {{prefix}}Value<{{typeParameter}}>({{valueType}} value)
                    => await value;
                private static async {{voidType}} {{prefix}}Void({{voidType}} value)
                    { await value; }
                private static async {{valueType}} {{prefix}}ConfiguredValue<{{typeParameter}}>({{valueType}} value)
                    => {{FormatAwait("value", useConfigureAwait: true)}};
                private static async {{voidType}} {{prefix}}ConfiguredVoid({{voidType}} value)
                    { {{FormatAwait("value", useConfigureAwait: true)}}; }
            }
            """;
        var tree = CSharpSyntaxTree.ParseText(source, parseOptions, cancellationToken: cancellationToken);
        var model = compilation.AddSyntaxTrees(tree).GetSemanticModel(tree);
        var probes = tree.GetRoot(cancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
        if (!IsSupported(probes[0], withValue: true) || !IsSupported(probes[1], withValue: false))
            return null;

        return new AwaitableTypes(withResult, withoutResult,
            configureWithResult: IsSupported(probes[2], withValue: true),
            configureWithoutResult: IsSupported(probes[3], withValue: false));

        bool IsSupported(MethodDeclarationSyntax probe, bool withValue)
        {
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

    public static string FormatAwait(string expression, bool useConfigureAwait) =>
        useConfigureAwait ? $"await {expression}.ConfigureAwait(false)" : $"await {expression}";
}
