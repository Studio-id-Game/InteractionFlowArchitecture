using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace InteractionFlow.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InteractionFlowAnalyzersAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "InteractionFlowArchitecture001";

        private static readonly LocalizableString Title =
    new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString MessageFormat =
            new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString Description =
            new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        //private static readonly LocalizableString Title = "Invalid layer dependency";
        //private static readonly LocalizableString MessageFormat = "Layer '{0}' must not depend on '{1}' (Type = '{2}')";
        //private static readonly LocalizableString Description = "Interaction Flow Architecture - Invalid layer dependency";
        private const string Category = "Architecture";

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private static readonly ImmutableDictionary<DiagnosticSeverity, DiagnosticDescriptor> RulesBySeverity =
            Enum.GetValues(typeof(DiagnosticSeverity))
                .Cast<DiagnosticSeverity>()
                .ToImmutableDictionary(
                    severity => severity,
                    severity => new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, severity, isEnabledByDefault: true, description: Description));

        private static DiagnosticDescriptor GetRule(DiagnosticSeverity severity)
        {
            return RulesBySeverity.TryGetValue(severity, out var rule) ? rule : Rule;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
               => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var analysisState = new CompilationAnalyzerContext(compilationContext.Options.AnalyzerConfigOptionsProvider);

                // ▼ 定義側
                compilationContext.RegisterSymbolAction(context => AnalyzeProperty(context, analysisState), SymbolKind.Property);
                compilationContext.RegisterSymbolAction(context => AnalyzeField(context, analysisState), SymbolKind.Field);
                compilationContext.RegisterSymbolAction(context => AnalyzeNamedType(context, analysisState), SymbolKind.NamedType);
                compilationContext.RegisterSymbolAction(context => AnalyzeMethod(context, analysisState), SymbolKind.Method);

                // ▼ 使用側
                compilationContext.RegisterOperationAction(context => AnalyzeOperation(context, analysisState),
                    OperationKind.ObjectCreation,
                    OperationKind.Invocation,
                    OperationKind.FieldReference,
                    OperationKind.PropertyReference,
                    OperationKind.VariableDeclarator);
            });
        }

        // =========================
        // Symbol（定義）
        // =========================

        private static void AnalyzeProperty(SymbolAnalysisContext context, CompilationAnalyzerContext analysisState)
        {
            var property = GetSymbol<IPropertySymbol>(context, out var location, out var sourceNamespace);
            var analysisContext = CreateAnalysisContext(context, location, sourceNamespace, analysisState);

            CheckTypeRecursive(analysisContext, property.Type);
        }

        private static void AnalyzeField(SymbolAnalysisContext context, CompilationAnalyzerContext analysisState)
        {
            var field = GetSymbol<IFieldSymbol>(context, out var location, out var sourceNamespace);
            var analysisContext = CreateAnalysisContext(context, location, sourceNamespace, analysisState);

            CheckTypeRecursive(analysisContext, field.Type);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context, CompilationAnalyzerContext analysisState)
        {
            var type = GetSymbol<INamedTypeSymbol>(context, out var location, out var sourceNamespace);
            var analysisContext = CreateAnalysisContext(context, location, sourceNamespace, analysisState);

            // 型パラメータ制約
            foreach (var tp in type.TypeParameters)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                foreach (var constraint in tp.ConstraintTypes)
                {
                    CheckTypeRecursive(analysisContext, constraint);
                }
            }

            // BaseType
            if (type.BaseType != null &&
                type.BaseType.SpecialType != SpecialType.System_Object)
            {
                CheckTypeRecursive(analysisContext, type.BaseType);
            }

            // Interfaces
            foreach (var iface in type.AllInterfaces)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                CheckTypeRecursive(analysisContext, iface);
            }
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context, CompilationAnalyzerContext analysisState)
        {
            var method = GetSymbol<IMethodSymbol>(context, out var location, out var sourceNamespace);
            var analysisContext = CreateAnalysisContext(context, location, sourceNamespace, analysisState);

            foreach (var param in method.Parameters)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                CheckTypeRecursive(analysisContext, param.Type);
            }

            foreach (var tp in method.TypeParameters)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                foreach (var constraint in tp.ConstraintTypes)
                {
                    CheckTypeRecursive(analysisContext, constraint);
                }
            }
        }

        private static T GetSymbol<T>(SymbolAnalysisContext context, out Location location, out string sourceNamespace)
            where T : ISymbol
        {
            var symbol = context.Symbol;
            location = symbol.Locations.FirstOrDefault(loc => loc.IsInSource) ?? Location.None;
            sourceNamespace = symbol.ContainingNamespace?.ToDisplayString() ?? "";
            return (T)symbol;
        }

        // =========================
        // Operation（使用）
        // =========================

        private static void AnalyzeOperation(OperationAnalysisContext context, CompilationAnalyzerContext analysisState)
        {
            var operation = context.Operation;
            var sourceNamespace = ResolveSourceNamespace(context, operation);
            var location = operation.Syntax.GetLocation();
            var analysisContext = CreateAnalysisContext(context, location, sourceNamespace, analysisState);

            if (operation.Type != null)
            {
                CheckTypeRecursive(analysisContext, operation.Type);
            }

            foreach (var dependencyType in GetOperationDependencyTypes(operation))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                CheckTypeRecursive(analysisContext, dependencyType);
            }
        }

        // =========================
        // 再帰型チェック（共通）
        // =========================


        private static void CheckTypeRecursive(AnalyzerExecutionContext context, ITypeSymbol type)
        {
            context.ThrowIfCancellationRequested();

            if (!context.IsEnabled()) return;
            else if (type == null) return;
            else if (context.IsVisited(type)) return;

            context.CheckAndReport(type);

            // ▼ ジェネリクス
            if (type is INamedTypeSymbol named)
            {
                foreach (var arg in named.TypeArguments)
                {
                    context.ThrowIfCancellationRequested();

                    CheckTypeRecursive(context, arg);
                }
            }

            // ▼ 配列
            if (type is IArrayTypeSymbol array)
            {
                CheckTypeRecursive(context, array.ElementType);
            }

            // ▼ Nullable<T>
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
                type is INamedTypeSymbol nullable)
            {
                var inner = nullable.TypeArguments.FirstOrDefault();
                if (inner != null)
                {
                    CheckTypeRecursive(context, inner);
                }
            }
        }

        private static string ResolveSourceNamespace(OperationAnalysisContext context, IOperation operation)
        {
            var symbolNamespace = context.ContainingSymbol?.ContainingNamespace?.ToDisplayString() ?? "";
            if (symbolNamespace.Length != 0)
            {
                return symbolNamespace;
            }

            var enclosingNamespace = operation.SemanticModel?
                .GetEnclosingSymbol(operation.Syntax.SpanStart, context.CancellationToken)?
                .ContainingNamespace?
                .ToDisplayString();

            return enclosingNamespace ?? "";
        }

        private static IEnumerable<ITypeSymbol> GetOperationDependencyTypes(IOperation operation)
        {
            switch (operation)
            {
                case IInvocationOperation invocation when invocation.TargetMethod?.ContainingType != null:
                    yield return invocation.TargetMethod.ContainingType;
                    break;
                case IObjectCreationOperation creation when creation.Constructor?.ContainingType != null:
                    yield return creation.Constructor.ContainingType;
                    break;
                case IFieldReferenceOperation fieldReference when fieldReference.Field?.ContainingType != null:
                    yield return fieldReference.Field.ContainingType;
                    break;
                case IPropertyReferenceOperation propertyReference when propertyReference.Property?.ContainingType != null:
                    yield return propertyReference.Property.ContainingType;
                    break;
                case IVariableDeclaratorOperation variable when variable.Symbol?.Type != null:
                    yield return variable.Symbol.Type;
                    break;
            }
        }

        // =========================
        // 実行コンテキスト
        // =========================

        private static AnalyzerExecutionContext CreateAnalysisContext(
            SymbolAnalysisContext context,
            Location location,
            string sourceNamespace,
            CompilationAnalyzerContext analysisState)
        {
            return new AnalyzerExecutionContext(
                context.ReportDiagnostic,
                location,
                sourceNamespace,
                analysisState.GetOptions(location),
                context.CancellationToken,
                analysisState);
        }

        private static AnalyzerExecutionContext CreateAnalysisContext(
            OperationAnalysisContext context,
            Location location,
            string sourceNamespace,
            CompilationAnalyzerContext analysisState)
        {
            return new AnalyzerExecutionContext(
                context.ReportDiagnostic,
                location,
                sourceNamespace,
                analysisState.GetOptions(context.Operation.Syntax.SyntaxTree),
                context.CancellationToken,
                analysisState);
        }

        private sealed class CompilationAnalyzerContext(AnalyzerConfigOptionsProvider optionsProvider)
        {
            private readonly OptionValues disabledOptions = new(null);
            private readonly ConcurrentDictionary<SyntaxTree, OptionValues> optionsByTree = new();
            private readonly ConcurrentDictionary<string, DisallowReferenceInfo> disallowReferenceCache = new(StringComparer.Ordinal);

            public OptionValues GetOptions(Location location)
            {
                var tree = location.SourceTree;
                return tree == null ? disabledOptions : GetOptions(tree);
            }

            public OptionValues GetOptions(SyntaxTree tree)
            {
                return optionsByTree.GetOrAdd(tree, currentTree => new OptionValues(optionsProvider.GetOptions(currentTree)));
            }

            public DisallowReferenceInfo GetDisallowReferenceInfo(OptionValues options, string sourceNamespace, string targetNamespace)
            {
                var cacheKey = string.Concat(options.AllowedRootsKey, "\u001f", sourceNamespace, "\u001f", targetNamespace);
                return disallowReferenceCache.GetOrAdd(cacheKey, _ =>
                {
                    var isDisallow = LayerNames.IsDisallowReference(
                        options.AllowedRoots,
                        sourceNamespace,
                        targetNamespace,
                        out var sourceShowName,
                        out var targetShowName);

                    return new DisallowReferenceInfo(targetNamespace, isDisallow, sourceShowName, targetShowName);
                });
            }
        }

        private sealed class DisallowReferenceInfo(string targetNamespace, bool isDisallow, string sourceShowName, string targetShowName)
        {
            public string TargetNamespace { get; } = targetNamespace;

            public bool IsDisallow { get; } = isDisallow;

            public string SourceShowName { get; } = sourceShowName;

            public string TargetShowName { get; } = targetShowName;
        }

        private sealed class AnalyzerExecutionContext(
            Action<Diagnostic> reportDiagnostic,
            Location location,
            string sourceNamespace,
            OptionValues options,
            CancellationToken cancellationToken,
            CompilationAnalyzerContext analysisState)
        {
            private readonly Action<Diagnostic> reportDiagnostic = reportDiagnostic;
            private readonly Location location = location;
            private readonly string sourceNamespace = sourceNamespace;
            private readonly OptionValues options = options;
            private readonly DiagnosticDescriptor rule = GetRule(options.Mode);
            private readonly HashSet<ITypeSymbol> visited = new(SymbolEqualityComparer.Default);
            private readonly CancellationToken cancellationToken = cancellationToken;
            private readonly CompilationAnalyzerContext analysisState = analysisState;

            public void ThrowIfCancellationRequested()
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            public bool IsEnabled()
            {
                return options.Enabled;
            }

            public bool IsVisited(ITypeSymbol type)
            {
                return !visited.Add(type);
            }

            public bool CheckAndReport(ITypeSymbol type)
            {
                var targetNamespaceSymbol = type.ContainingNamespace;
                if (targetNamespaceSymbol == null || targetNamespaceSymbol.IsGlobalNamespace)
                {
                    return false;
                }

                var targetNamespace = targetNamespaceSymbol.ToDisplayString();
                if (string.IsNullOrEmpty(targetNamespace)) return false;

                var disallowReferenceInfo = analysisState.GetDisallowReferenceInfo(options, sourceNamespace, targetNamespace);

                if (disallowReferenceInfo.IsDisallow)
                {
                    var args = new string[]
                    {
                        disallowReferenceInfo.SourceShowName,
                        disallowReferenceInfo.TargetShowName,
                        type.Name
                    };

                    reportDiagnostic(Diagnostic.Create(rule, location, args));
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
