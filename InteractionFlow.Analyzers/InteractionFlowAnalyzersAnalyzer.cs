using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    /// <summary>
    /// Analyzes InteractionFlow layer dependencies and IDependencyNode dependency declarations.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]

    public class InteractionFlowAnalyzersAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic identifier for invalid Interaction Flow architecture layer dependencies.
        /// </summary>
        public const string DiagnosticId = "InteractionFlowArchitecture001";

        /// <summary>
        /// The diagnostic identifier for incomplete IDependencyNode dependency declarations.
        /// </summary>
        public const string DependencyNodeDiagnosticId = "InteractionFlowArchitecture002";

        private static readonly LocalizableString LayerDependencyAnalyzerTitle =
            new LocalizableResourceString(nameof(Resources.LayerDependencyAnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString LayerDependencyAnalyzerMessageFormat =
            new LocalizableResourceString(nameof(Resources.LayerDependencyAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString LayerDependencyAnalyzerDescription =
            new LocalizableResourceString(nameof(Resources.LayerDependencyAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString DependencyNodeAnalyzerTitle =
            new LocalizableResourceString(nameof(Resources.DependencyNodeAnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString DependencyNodeAnalyzerMessageFormat =
            new LocalizableResourceString(nameof(Resources.DependencyNodeAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString DependencyNodeAnalyzerDescription =
            new LocalizableResourceString(nameof(Resources.DependencyNodeAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = "Architecture";

        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId,
            LayerDependencyAnalyzerTitle,
            LayerDependencyAnalyzerMessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: LayerDependencyAnalyzerDescription);

        private static readonly DiagnosticDescriptor DependencyNodeRule = new(
            DependencyNodeDiagnosticId,
            DependencyNodeAnalyzerTitle,
            DependencyNodeAnalyzerMessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: DependencyNodeAnalyzerDescription);

        private static readonly ImmutableDictionary<DiagnosticSeverity, DiagnosticDescriptor> RulesBySeverity =
            Enum.GetValues(typeof(DiagnosticSeverity))
                .Cast<DiagnosticSeverity>()
                .ToImmutableDictionary(
                    severity => severity,
                    severity => new DiagnosticDescriptor(
                        DiagnosticId,
                        Rule.Title,
                        Rule.MessageFormat,
                        Category,
                        severity,
                        isEnabledByDefault: true,
                        description: Rule.Description));

        private static readonly ImmutableDictionary<DiagnosticSeverity, DiagnosticDescriptor> DependencyNodeRulesBySeverity =
            Enum.GetValues(typeof(DiagnosticSeverity))
                .Cast<DiagnosticSeverity>()
                .ToImmutableDictionary(
                    severity => severity,
                    severity => new DiagnosticDescriptor(
                        DependencyNodeDiagnosticId,
                        DependencyNodeRule.Title,
                        DependencyNodeRule.MessageFormat,
                        Category,
                        severity,
                        isEnabledByDefault: true,
                        description: DependencyNodeRule.Description));

        private static DiagnosticDescriptor GetRule(DiagnosticSeverity severity)
        {
            return RulesBySeverity.TryGetValue(severity, out var rule) ? rule : Rule;
        }

        private static DiagnosticDescriptor GetDependencyNodeRule(DiagnosticSeverity severity)
        {
            return DependencyNodeRulesBySeverity.TryGetValue(severity, out var rule) ? rule : DependencyNodeRule;
        }

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
               => ImmutableArray.Create(Rule, DependencyNodeRule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var analysisState = new CompilationAnalyzerContext(
                    compilationContext.Compilation,
                    compilationContext.Options.AnalyzerConfigOptionsProvider);

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

            AnalyzeDependencyNodeType(context, type, analysisState);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context, CompilationAnalyzerContext analysisState)
        {
            var method = GetSymbol<IMethodSymbol>(context, out var location, out var sourceNamespace);
            if (method.AssociatedSymbol is IPropertySymbol)
            {
                return;
            }

            var analysisContext = CreateAnalysisContext(context, location, sourceNamespace, analysisState);

            CheckTypeRecursive(analysisContext, method.ReturnType);

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
        // Dependency Node
        // =========================

        private static void AnalyzeDependencyNodeType(
            SymbolAnalysisContext context,
            INamedTypeSymbol type,
            CompilationAnalyzerContext analysisState)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var dependencyNode = analysisState.DependencyNodeType;
            if (dependencyNode == null ||
                type.TypeKind != TypeKind.Class ||
                !IsDependencyNodeType(type, dependencyNode))
            {
                return;
            }

            var typeLocation = type.Locations.FirstOrDefault(loc => loc.IsInSource) ?? Location.None;
            var options = analysisState.GetOptions(typeLocation);
            if (!options.Enabled)
            {
                return;
            }

            var baseTypeIsDependencyNode = type.BaseType != null &&
                type.BaseType.SpecialType != SpecialType.System_Object &&
                IsDependencyNodeType(type.BaseType, dependencyNode);

            foreach (var constructor in type.Constructors.Where(e => !e.IsStatic))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (!type.IsSealed && !HasParamsDependencyNodeParameter(constructor, dependencyNode))
                {
                    var location = GetConstructorDiagnosticLocation(constructor, typeLocation);
                    ReportDependencyNodeDiagnostic(
                        context,
                        analysisState,
                        location,
                        Resources.DependencyNodeMustBeSealedOrHaveParams);
                }

                foreach (var parameter in constructor.Parameters.Where(e => IsDependencyParameter(e, dependencyNode)))
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    if (baseTypeIsDependencyNode)
                    {
                        if (!ConstructorBaseCallIncludesParameter(context, analysisState, constructor, parameter))
                        {
                            ReportDependencyNodeDiagnostic(
                                context,
                                analysisState,
                                GetParameterDiagnosticLocation(parameter, constructor, typeLocation),
                                string.Format(Resources.DependencyNodeParameterMustBePassedToBase, parameter.Name));
                        }
                    }
                    else if (!DependencyPropertyIncludesParameter(context, analysisState, type, dependencyNode, constructor, parameter))
                    {
                        ReportDependencyNodeDiagnostic(
                            context,
                            analysisState,
                            GetParameterDiagnosticLocation(parameter, constructor, typeLocation),
                            string.Format(Resources.DependencyNodeParameterMustBeIncludedInDependency, parameter.Name));
                    }
                }
            }
        }

        private static bool IsDependencyParameter(IParameterSymbol parameter, INamedTypeSymbol dependencyNode)
        {
            return IsDependencyNodeType(parameter.Type, dependencyNode);
        }

        private static bool IsDependencyNodeType(ITypeSymbol type, INamedTypeSymbol dependencyNode)
        {
            if (type is IArrayTypeSymbol array)
            {
                return IsDependencyNodeType(array.ElementType, dependencyNode);
            }

            if (SymbolEqualityComparer.Default.Equals(type, dependencyNode))
            {
                return true;
            }

            if (type is ITypeParameterSymbol typeParameter)
            {
                return typeParameter.ConstraintTypes.Any(e => IsDependencyNodeType(e, dependencyNode));
            }

            if (type is INamedTypeSymbol named)
            {
                return named.AllInterfaces.Any(e => SymbolEqualityComparer.Default.Equals(e, dependencyNode));
            }

            return false;
        }

        private static bool HasParamsDependencyNodeParameter(IMethodSymbol constructor, INamedTypeSymbol dependencyNode)
        {
            return constructor.Parameters.Any(parameter =>
                parameter.IsParams &&
                parameter.Type is IArrayTypeSymbol array &&
                SymbolEqualityComparer.Default.Equals(array.ElementType, dependencyNode));
        }

        private static bool ConstructorBaseCallIncludesParameter(
            SymbolAnalysisContext context,
            CompilationAnalyzerContext analysisState,
            IMethodSymbol constructor,
            IParameterSymbol parameter)
        {
            foreach (var syntaxReference in constructor.DeclaringSyntaxReferences)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var syntax = syntaxReference.GetSyntax(context.CancellationToken);
                var model = analysisState.GetSemanticModel(syntax.SyntaxTree);

                if (syntax is ConstructorDeclarationSyntax constructorDeclaration)
                {
                    var initializer = constructorDeclaration.Initializer;
                    if (initializer != null &&
                        initializer.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.BaseConstructorInitializer) &&
                        ContainsSymbolReference(initializer, parameter, model, context.CancellationToken))
                    {
                        return true;
                    }
                }
                else if (syntax is TypeDeclarationSyntax typeDeclaration &&
                    typeDeclaration.BaseList != null)
                {
                    foreach (var baseType in typeDeclaration.BaseList.Types)
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();

                        if (baseType.ToString().Contains("(") &&
                            ContainsSymbolReference(baseType, parameter, model, context.CancellationToken))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool DependencyPropertyIncludesParameter(
            SymbolAnalysisContext context,
            CompilationAnalyzerContext analysisState,
            INamedTypeSymbol type,
            INamedTypeSymbol dependencyNode,
            IMethodSymbol constructor,
            IParameterSymbol parameter)
        {
            var dependencyProperties = type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(e => e.Parameters.Length == 0 && IsDependencyProperty(e, dependencyNode))
                .ToImmutableArray();

            if (dependencyProperties.Length == 0)
            {
                return false;
            }

            foreach (var property in dependencyProperties)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var referencedMembers = new HashSet<ISymbol>(SymbolEqualityComparer.Default)
                {
                    property
                };

                foreach (var syntaxReference in property.DeclaringSyntaxReferences)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    var syntax = syntaxReference.GetSyntax(context.CancellationToken);
                    var model = analysisState.GetSemanticModel(syntax.SyntaxTree);

                    if (ContainsSymbolReference(syntax, parameter, model, context.CancellationToken))
                    {
                        return true;
                    }

                    foreach (var referencedMember in GetReferencedMemberSymbols(syntax, model, type, context.CancellationToken))
                    {
                        referencedMembers.Add(referencedMember);
                    }
                }

                foreach (var referencedMember in referencedMembers)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    if (MemberDeclarationIncludesParameter(context, analysisState, referencedMember, parameter) ||
                        ConstructorAssignmentIncludesParameter(context, analysisState, constructor, referencedMember, parameter))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsDependencyProperty(IPropertySymbol property, INamedTypeSymbol dependencyNode)
        {
            var dependencyProperty = dependencyNode.GetMembers("Dependency")
                .OfType<IPropertySymbol>()
                .FirstOrDefault(e => e.Parameters.Length == 0);

            if (dependencyProperty == null)
            {
                return false;
            }

            if (property.ExplicitInterfaceImplementations.Any(e =>
                SymbolEqualityComparer.Default.Equals(e, dependencyProperty)))
            {
                return true;
            }

            var implementation = property.ContainingType.FindImplementationForInterfaceMember(dependencyProperty);
            return SymbolEqualityComparer.Default.Equals(implementation, property);
        }

        private static IEnumerable<ISymbol> GetReferencedMemberSymbols(
            SyntaxNode syntax,
            SemanticModel model,
            INamedTypeSymbol containingType,
            CancellationToken cancellationToken)
        {
            foreach (var node in syntax.DescendantNodesAndSelf())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var symbol = model.GetSymbolInfo(node, cancellationToken).Symbol;
                if ((symbol is IFieldSymbol || symbol is IPropertySymbol) &&
                    SymbolEqualityComparer.Default.Equals(symbol.ContainingType, containingType))
                {
                    yield return symbol;
                }
            }
        }

        private static bool MemberDeclarationIncludesParameter(
            SymbolAnalysisContext context,
            CompilationAnalyzerContext analysisState,
            ISymbol member,
            IParameterSymbol parameter)
        {
            foreach (var syntaxReference in member.DeclaringSyntaxReferences)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var syntax = syntaxReference.GetSyntax(context.CancellationToken);
                var model = analysisState.GetSemanticModel(syntax.SyntaxTree);
                if (ContainsSymbolReference(syntax, parameter, model, context.CancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ConstructorAssignmentIncludesParameter(
            SymbolAnalysisContext context,
            CompilationAnalyzerContext analysisState,
            IMethodSymbol constructor,
            ISymbol targetMember,
            IParameterSymbol parameter)
        {
            foreach (var syntaxReference in constructor.DeclaringSyntaxReferences)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (syntaxReference.GetSyntax(context.CancellationToken) is not ConstructorDeclarationSyntax constructorDeclaration)
                {
                    continue;
                }

                var body = (SyntaxNode?)constructorDeclaration.Body ?? constructorDeclaration.ExpressionBody;
                if (body == null)
                {
                    continue;
                }

                var model = analysisState.GetSemanticModel(body.SyntaxTree);
                foreach (var assignment in body.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>())
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    var leftSymbol = model.GetSymbolInfo(assignment.Left, context.CancellationToken).Symbol;
                    if (SymbolEqualityComparer.Default.Equals(leftSymbol, targetMember) &&
                        ContainsSymbolReference(assignment.Right, parameter, model, context.CancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ContainsSymbolReference(
            SyntaxNode syntax,
            ISymbol symbol,
            SemanticModel model,
            CancellationToken cancellationToken)
        {
            foreach (var node in syntax.DescendantNodesAndSelf())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var referencedSymbol = model.GetSymbolInfo(node, cancellationToken).Symbol;
                if (SymbolEqualityComparer.Default.Equals(referencedSymbol, symbol))
                {
                    return true;
                }
            }

            return false;
        }

        private static Location GetConstructorDiagnosticLocation(IMethodSymbol constructor, Location fallback)
        {
            return constructor.Locations.FirstOrDefault(loc => loc.IsInSource) ?? fallback;
        }

        private static Location GetParameterDiagnosticLocation(IParameterSymbol parameter, IMethodSymbol constructor, Location fallback)
        {
            return parameter.Locations.FirstOrDefault(loc => loc.IsInSource) ??
                GetConstructorDiagnosticLocation(constructor, fallback);
        }

        private static void ReportDependencyNodeDiagnostic(
            SymbolAnalysisContext context,
            CompilationAnalyzerContext analysisState,
            Location location,
            string message)
        {
            var options = analysisState.GetOptions(location);
            if (!options.Enabled)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(GetDependencyNodeRule(options.Mode), location, message));
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

        private sealed class CompilationAnalyzerContext(Compilation compilation, AnalyzerConfigOptionsProvider optionsProvider)
        {
            private readonly OptionValues disabledOptions = new(null);
            private readonly Compilation compilation = compilation;
            private readonly ConcurrentDictionary<SyntaxTree, OptionValues> optionsByTree = new();
            private readonly ConcurrentDictionary<SyntaxTree, SemanticModel> semanticModelByTree = new();
            private readonly ConcurrentDictionary<string, DisallowReferenceInfo> disallowReferenceCache = new(StringComparer.Ordinal);

            public INamedTypeSymbol? DependencyNodeType { get; } =
                compilation.GetTypeByMetadataName("InteractionFlow.Core.Entities.Architectures.IDependencyNode");

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

            public SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
            {
                return semanticModelByTree.GetOrAdd(syntaxTree, tree => compilation.GetSemanticModel(tree));
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
                    var detailMessage = string.Format(
                        Resources.LayerDependencyDisallowedReference,
                        disallowReferenceInfo.SourceShowName,
                        disallowReferenceInfo.TargetShowName,
                        type.Name);

                    reportDiagnostic(Diagnostic.Create(rule, location, detailMessage));
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
