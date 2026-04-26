using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private static DiagnosticDescriptor GetRule(DiagnosticSeverity severity)
        {
            return new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, severity, isEnabledByDefault: true, description: Description);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
               => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // ▼ 定義側
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);

            // ▼ 使用側
            context.RegisterOperationAction(AnalyzeOperation,
                OperationKind.ObjectCreation,
                OperationKind.Invocation,
                OperationKind.FieldReference,
                OperationKind.PropertyReference,
                OperationKind.VariableDeclarator);
        }

        // =========================
        // Symbol（定義）
        // =========================

        private static void AnalyzeProperty(SymbolAnalysisContext context)
        {
            var property = GetSymbol<IPropertySymbol>(context, out var location, out var sourceNamespace);
            var analysisContext = CreateAnalysisContext(context, location, sourceNamespace);

            CheckTypeRecursive(analysisContext, property.Type);
        }

        private static void AnalyzeField(SymbolAnalysisContext context)
        {
            var field = GetSymbol<IFieldSymbol>(context, out var location, out var sourceNamespace);
            var analysisContext = CreateAnalysisContext(context, location, sourceNamespace);

            CheckTypeRecursive(analysisContext, field.Type);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var type = GetSymbol<INamedTypeSymbol>(context, out var location, out var sourceNamespace);
            var analysisContext = CreateAnalysisContext(context, location, sourceNamespace);

            // 型パラメータ制約
            foreach (var tp in type.TypeParameters)
            {
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
                CheckTypeRecursive(analysisContext, iface);
            }
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = GetSymbol<IMethodSymbol>(context, out var location, out var sourceNamespace);
            var analysisContext = CreateAnalysisContext(context, location, sourceNamespace);

            foreach (var param in method.Parameters)
            {
                CheckTypeRecursive(analysisContext, param.Type);
            }

            foreach (var tp in method.TypeParameters)
            {
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
            location = symbol.Locations.FirstOrDefault(loc => loc.IsInSource);
            sourceNamespace = symbol.ContainingNamespace?.ToDisplayString() ?? "";
            return (T)symbol;
        }

        // =========================
        // Operation（使用）
        // =========================

        private static void AnalyzeOperation(OperationAnalysisContext context)
        {
            var operation = context.Operation;
            var sourceNamespace = ResolveSourceNamespace(context, operation);
            var location = operation.Syntax.GetLocation();
            var analysisContext = CreateAnalysisContext(context, location, sourceNamespace);

            if (operation.Type != null)
            {
                CheckTypeRecursive(analysisContext, operation.Type);
            }

            foreach (var dependencyType in GetOperationDependencyTypes(operation))
            {
                CheckTypeRecursive(analysisContext, dependencyType);
            }
        }

        // =========================
        // 再帰型チェック（共通）
        // =========================


        private static void CheckTypeRecursive(AnalyzerExecutionContext context, ITypeSymbol type)
        {
            if (!context.IsEnabled()) return;
            else if (type == null) return;
            else if (context.IsVisited(type)) return;

            context.CheckAndReport(type);

            // ▼ ジェネリクス
            if (type is INamedTypeSymbol named)
            {
                foreach (var arg in named.TypeArguments)
                {
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
            var symbolNamespace = context.ContainingSymbol?.ContainingNamespace?.ToDisplayString();
            if (!string.IsNullOrEmpty(symbolNamespace))
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
        // 共通インターフェース化
        // =========================

        private static AnalyzerExecutionContext CreateAnalysisContext(SymbolAnalysisContext context, Location location, string sourceNamespace)
        {
            var wrapper = new SymbolContextWrapper(context);
            return new AnalyzerExecutionContext(wrapper, location, sourceNamespace);
        }

        private static AnalyzerExecutionContext CreateAnalysisContext(OperationAnalysisContext context, Location location, string sourceNamespace)
        {
            var wrapper = new OperationContextWrapper(context);
            return new AnalyzerExecutionContext(wrapper, location, sourceNamespace);
        }

        private sealed class AnalyzerExecutionContext
        {
            private class DisallowReferenceInfo
            {
                public DisallowReferenceInfo(string targetNamespace, bool isDisallow, string sourceShowName, string targetShowName)
                {
                    TargetNamespace = targetNamespace;
                    IsDisallow = isDisallow;
                    SourceShowName = sourceShowName;
                    TargetShowName = targetShowName;
                }

                public string TargetNamespace { get; }

                public bool IsDisallow { get; }

                public string SourceShowName { get; }

                public string TargetShowName { get; }
            }


            private readonly AnalysisContextBase context;

            private readonly Location location;

            private readonly string sourceNamespace;

            private readonly OptionValues options;

            private readonly DiagnosticDescriptor rule;

            private readonly HashSet<ITypeSymbol> visited;

            private readonly Dictionary<string, DisallowReferenceInfo> disallowReferenceCach = new Dictionary<string, DisallowReferenceInfo>(StringComparer.Ordinal);

            public AnalyzerExecutionContext(AnalysisContextBase context, Location location, string sourceNamespace)
            {
                this.context = context;
                this.location = location;
                this.sourceNamespace = sourceNamespace;
                options = new OptionValues(context.GetOptions());
                rule = GetRule(options.Mode);
                visited = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
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
                var targetNamespace = type.ContainingNamespace?.ToDisplayString();

                if (string.IsNullOrEmpty(targetNamespace)) return false;

                if (!disallowReferenceCach.TryGetValue(targetNamespace, out var disallowReferenceInfo))
                {

                    var isDisallow = IsDisallowReference(targetNamespace, out var sourceShowName, out var targetShowName);
                    disallowReferenceCach[targetNamespace] =
                        disallowReferenceInfo =
                        new DisallowReferenceInfo(targetNamespace, isDisallow, sourceShowName, targetShowName);
                }

                if (disallowReferenceInfo.IsDisallow)
                {
                    var args = new string[]
                    {
                        disallowReferenceInfo.SourceShowName,
                        disallowReferenceInfo.TargetShowName,
                        type.Name
                    };

                    context.ReportDiagnostic(Diagnostic.Create(rule, location, args));
                    return true;
                }
                else
                {
                    return false;
                }
            }

            private bool IsDisallowReference(string targetNamespace, out string sourceShowName, out string targetShowName)
            {
                return LayerNames.IsDisallowReference(options.AllowedRoots, sourceNamespace, targetNamespace, out sourceShowName, out targetShowName);
            }
        }

        private abstract class AnalysisContextBase
        {
            public abstract void ReportDiagnostic(Diagnostic diagnostic);

            public abstract AnalyzerConfigOptions GetOptions();
        }

        private class SymbolContextWrapper : AnalysisContextBase
        {
            private readonly SymbolAnalysisContext _context;

            public SymbolContextWrapper(SymbolAnalysisContext context) => _context = context;

            public override void ReportDiagnostic(Diagnostic diagnostic) => _context.ReportDiagnostic(diagnostic);

            public override AnalyzerConfigOptions GetOptions()
            {
                var location = _context.Symbol.Locations.FirstOrDefault();
                if (location == null || !location.IsInSource) return null;

                var tree = location.SourceTree;
                var provider = _context.Options.AnalyzerConfigOptionsProvider;

                return provider.GetOptions(tree);
            }

        }

        private class OperationContextWrapper : AnalysisContextBase
        {
            private readonly OperationAnalysisContext _context;

            public OperationContextWrapper(OperationAnalysisContext context) => _context = context;

            public override void ReportDiagnostic(Diagnostic diagnostic) => _context.ReportDiagnostic(diagnostic);

            public override AnalyzerConfigOptions GetOptions()
            {
                var tree = _context.Operation.Syntax.SyntaxTree;
                var provider = _context.Options.AnalyzerConfigOptionsProvider;

                return provider.GetOptions(tree);
            }
        }
    }
}
