using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed class AnalysisInteraction : IAnalysisInteraction
{
    public InteractionResult<AnalyzedExtension> Analyze(
        DetectedExtension extension,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var context = new AnalysisContext(extension, compilation, cancellationToken);
        var destination = extension.Definition.Destination;
        var analysis = AnalyzeTemplates(context);
        var templates = analysis.Value;
        if (templates.IsDefaultOrEmpty || extension.Targets.IsDefaultOrEmpty)
            return new InteractionResult<AnalyzedExtension>(new AnalyzedExtension(destination, default, default),
                analysis.Diagnostics);

        var candidates = ImmutableArray.CreateBuilder<AnalyzedCandidate>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        AddDiagnostics(analysis.Diagnostics);

        var boundTargets = ImmutableArray.CreateBuilder<BoundTarget>();
        foreach (var target in extension.Targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (BindResultTypes(target, context) is { } boundTarget)
                boundTargets.Add(boundTarget);
        }

        foreach (var template in templates)
        {
            foreach (var boundTarget in boundTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!template.Method.CanApplyTo(boundTarget.Target))
                    continue;

                var (factories, factoryDiagnostics) = ResolveFactories(context, template, boundTarget);
                AddDiagnostics(factoryDiagnostics);

                var (candidate, candidateDiagnostics) = factories.IsDefault
                    ? SkipCandidate()
                    : AnalyzeCallbackArguments(context, template, boundTarget, factories);
                AddDiagnostics(candidateDiagnostics);

                if (candidate is not null)
                    candidates.Add(candidate);
            }
        }

        var handwrittenMembers = extension.DestinationSymbol.GetMembers()
            .Where(static member => member is not IMethodSymbol or IMethodSymbol { MethodKind: MethodKind.Ordinary })
            .Select(HandwrittenSignatures.Describe)
            .ToImmutableArray();
        return new InteractionResult<AnalyzedExtension>(
            new AnalyzedExtension(destination, candidates.ToImmutable(), handwrittenMembers),
            diagnostics.ToImmutable());

        void AddDiagnostics(ImmutableArray<Diagnostic> items)
        {
            if (!items.IsDefaultOrEmpty)
                diagnostics.AddRange(items);
        }
    }

    private static InteractionResult<ImmutableArray<ParsedTemplate>> AnalyzeTemplates(AnalysisContext context)
    {
        var templates = ImmutableArray.CreateBuilder<ParsedTemplate>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var placeholders = TemplatePlaceholders.Find(context.Extension.Definition.Template);
        foreach (var method in context.Extension.Definition.Template.GetMembers().OfType<IMethodSymbol>())
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (ResultApiAttributes.Find(method, ResultApiAttributes.Function) is not { } attribute)
                continue;

            var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken);
            var arguments = attribute.ConstructorArguments;
            if (placeholders is null || syntax is not MethodDeclarationSyntax { Body: { } } declaration
                || !method.IsStatic || method.Parameters.IsEmpty || !placeholders.IsSelf(method.Parameters[0].Type)
                || method.ReturnsByRef || method.ReturnsByRefReadonly
                || method.TypeParameters.Any(TypeNames.HasConstraints)
                || !TypeNames.IsIdentifier(method.Name + context.Extension.Definition.Suffix)
                || method.Parameters.Any(static parameter => parameter.RefKind != RefKind.None)
                || arguments.Length != 2 || arguments[0].Value is not bool asyncSelf
                || arguments[1].Value is not bool asyncCallback)
            {
                diagnostics.Add(GeneratorDiagnostics.Create(syntax?.GetLocation(),
                    "The template must be a static block-bodied method with a Self receiver and supported parameters."));
                continue;
            }

            var model = context.Compilation.GetSemanticModel(declaration.SyntaxTree);
            if (model.GetOperation(declaration.Body!, context.CancellationToken) is not IBlockOperation body)
                continue;

            var operations = body.DescendantsAndSelf().ToImmutableArray();
            var invocations = operations.OfType<IInvocationOperation>().ToImmutableArray();
            var factories = invocations
                .Where(call => placeholders.IsOther(call.TargetMethod.ContainingType))
                .ToImmutableArray();
            var usesOther = placeholders.IsOther(method.ReturnType)
                || method.Parameters.Any(parameter => placeholders.IsOther(parameter.Type)
                    || (placeholders.TryGetCallback(parameter.Type, out _, out var result)
                        && placeholders.IsOther(result)))
                || operations.Any(operation => placeholders.IsOther(operation.Type))
                || !factories.IsEmpty;
            templates.Add(new ParsedTemplate(
                new TemplateMethod(method, usesOther, asyncSelf, asyncCallback),
                body, placeholders, factories, invocations, operations.OfType<IPropertyReferenceOperation>()
                    .Any(property => placeholders.IsSuccessProperty(property.Property))));
        }

        return new InteractionResult<ImmutableArray<ParsedTemplate>>(templates.ToImmutable(), diagnostics.ToImmutable());
    }

    private static BoundTarget? BindResultTypes(ResultTarget target, AnalysisContext context)
    {
        var names = target.GetTypeParameterNames();
        var typeArguments = new Dictionary<string, ITypeSymbol>(names.Length, System.StringComparer.Ordinal);
        // Use distinct unconstrained symbols from this compilation for generated type variables.
        var variables = context.TypeVariables;
        if (!names.IsEmpty && (variables.IsDefault || names.Length > variables.Length))
            return null;

        for (var index = 0; index < names.Length; index++)
            typeArguments.Add(names[index], variables[index]);

        return target.Self.Bind(typeArguments) is { } self && target.Other.Bind(typeArguments) is { } other
            ? new BoundTarget(target, self, other, typeArguments, context)
            : null;
    }

    /// <summary>成立しなければ Factories は default、ファクトリが不要なら空です。</summary>
    private static (ImmutableArray<ResolvedFactoryCall> Factories,
        ImmutableArray<Diagnostic> Diagnostics) ResolveFactories(
        AnalysisContext context,
        ParsedTemplate template,
        BoundTarget boundTarget)
    {
        var factories = ImmutableArray.CreateBuilder<ResolvedFactoryCall>(template.Factories.Length);
        foreach (var call in template.Factories)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (!UsesReceiver(call, template.Method.Method.Parameters[0])
                || !call.TargetMethod.IsStatic
                || call.TargetMethod.Name is not ("CreateSuccess" or "CreateFailure"))
            {
                return Invalid("A factory placeholder must receive only the template receiver.");
            }

            var matches = boundTarget.FindFactories(call.TargetMethod, call.TargetMethod.Name == "CreateSuccess");
            if (matches.IsEmpty)
                return default;

            var best = matches.Length == 1
                ? matches[0]
                : matches.SingleOrDefault(candidate => matches.All(other =>
                    SymbolEqualityComparer.Default.Equals(candidate.Factory, other.Factory)
                    || IsBetterFactory(candidate.Factory, other.Factory, context.Compilation)));
            if (best.Factory is null)
                return Invalid($"Factory '{call.TargetMethod.Name}' is ambiguous for '{boundTarget.Other}'.");

            factories.Add(new ResolvedFactoryCall(call, best.Factory, best.Arguments));

            (ImmutableArray<ResolvedFactoryCall>, ImmutableArray<Diagnostic>) Invalid(string message) =>
                (default, ImmutableArray.Create(GeneratorDiagnostics.Create(call.Syntax.GetLocation(), message)));
        }

        return (factories.MoveToImmutable(), default);
    }

    private static bool IsBetterFactory(IMethodSymbol candidate, IMethodSymbol other, Compilation compilation)
    {
        if (candidate.Parameters.Length != other.Parameters.Length)
            return false;

        var strictlyBetter = false;
        foreach (var parameter in candidate.Parameters)
        {
            var counterpart = other.Parameters.FirstOrDefault(item => item.Name == parameter.Name);
            if (counterpart is null)
                return false;

            var conversion = compilation.ClassifyCommonConversion(parameter.Type, counterpart.Type);
            if (conversion.IsIdentity)
                continue;
            if (!conversion.IsImplicit || !conversion.IsReference)
                return false;
            strictlyBetter = true;
        }

        return strictlyBetter;
    }

    private static (AnalyzedCandidate? Candidate, ImmutableArray<Diagnostic> Diagnostics) SkipCandidate()
        => (null, ImmutableArray<Diagnostic>.Empty);

    private static (AnalyzedCandidate? Candidate, ImmutableArray<Diagnostic> Diagnostics) AnalyzeCallbackArguments(
        AnalysisContext context,
        ParsedTemplate template,
        BoundTarget boundTarget,
        ImmutableArray<ResolvedFactoryCall> factories)
    {
        var method = template.Method.Method;
        var placeholders = template.Placeholders;
        var receiver = method.Parameters[0];
        var receiverName = TypeNames.Escape(receiver.Name);
        var definition = context.Extension.Definition;
        var awaitables = definition.Awaitables;
        if (template.UsesIsSuccess && !boundTarget.HasSuccessCheck)
            return Invalid($"Receiver '{boundTarget.Self}' must expose a readable bool IsSuccess property.");

        var bindings = new CandidateBindings(template, boundTarget);
        foreach (var resolved in factories)
            bindings.AddFactory(resolved);

        var callbackError = BindCallbacks(context.Compilation, bindings, awaitables);
        if (callbackError is not null)
            return Invalid(callbackError);

        var body = bindings.WriteBody(awaitCallbacks: false);
        if (body is null)
            return Invalid("The template body contains an unsupported operation.");

        var resolvedReturn = bindings.BindType(method.ReturnType);
        var awaitedResult = awaitables?.Wrap(resolvedReturn);
        var awaitedReceiver = template.Method.SupportsAwait(awaitReceiver: true, awaitCallbacks: false)
            ? awaitables?.Wrap(boundTarget.Self) : null;
        var awaitedBody = bindings.CanAwaitCallbacks ? bindings.WriteBody(awaitCallbacks: true) : null;
        var xml = method.GetDocumentationCommentXml(cancellationToken: context.CancellationToken);
        return (new AnalyzedCandidate(
            TypeNames.Escape(definition.GetMethodName(template.Method)),
            bindings.DescribeParameter(receiverName, boundTarget.Self, awaitedReceiver),
            bindings.FormatType(resolvedReturn), awaitedResult is null ? null : bindings.FormatType(awaitedResult),
            bindings.TypeParameters, bindings.Parameters.MoveToImmutable(), body, awaitedBody, xml,
            bindings.FormatType(resolvedReturn, forSignature: true),
            awaitedResult is null ? null : bindings.FormatType(awaitedResult, forSignature: true)),
            default);

        (AnalyzedCandidate?, ImmutableArray<Diagnostic>) Invalid(string message) =>
            (null, ImmutableArray.Create(GeneratorDiagnostics.Create(method.Locations.FirstOrDefault(), message)));
    }

    private static string? BindCallbacks(
        Compilation compilation, CandidateBindings bindings, AwaitableTypes? awaitables)
    {
        var template = bindings.Template;
        var boundTarget = bindings.Target;
        var receiver = bindings.Receiver;
        var callbackCalls = template.Invocations
            .Where(static call => call.Instance is IParameterReferenceOperation)
            .ToLookup(call => ((IParameterReferenceOperation)call.Instance!).Parameter, SymbolEqualityComparer.Default);
        foreach (var parameter in template.Method.Method.Parameters.Skip(1))
        {
            if (!template.Placeholders.TryGetCallback(parameter.Type, out var forSuccess, out var callbackReturn))
                return $"Unsupported template parameter '{parameter.Name}'.";

            var properties = boundTarget.GetProperties(forSuccess);
            var argumentTypes = properties.Select(boundTarget.GetPropertyType).ToImmutableArray();
            var returnType = callbackReturn is null ? null : bindings.BindType(callbackReturn);
            var delegateType = GetDelegateType(compilation, argumentTypes, returnType);
            if (delegateType is null)
                return $"The callback '{parameter.Name}' cannot be represented by a delegate.";

            INamedTypeSymbol? awaitedDelegate = null;
            if (awaitables is not null && template.Method.SupportsAwait(awaitReceiver: false, awaitCallbacks: true))
            {
                var awaitedReturn = returnType is null ? awaitables.WithoutResult : awaitables.Wrap(returnType);
                awaitedDelegate = GetDelegateType(compilation, argumentTypes, awaitedReturn);
            }

            var name = TypeNames.Escape(parameter.Name);
            bindings.Parameters.Add(bindings.DescribeParameter(name, delegateType, awaitedDelegate));
            var arguments = string.Join(", ", properties.Select(bindings.FormatProperty));
            foreach (var call in callbackCalls[parameter])
            {
                if (call.TargetMethod.Name != "Invoke" || !UsesReceiver(call, receiver))
                    return $"The callback '{parameter.Name}' must be invoked with the template receiver.";
                bindings.AddCallback(call, $"{name}({arguments})");
            }
        }

        return null;
    }

    private sealed class CandidateBindings
    {
        private readonly Dictionary<ITypeParameterSymbol, string> typeNames = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<IInvocationOperation, (string Expression, bool IsCallback)> calls = new();

        public CandidateBindings(ParsedTemplate template, BoundTarget target)
        {
            Template = template;
            Target = target;
            var method = template.Method.Method;
            Receiver = method.Parameters[0];
            Parameters = ImmutableArray.CreateBuilder<AnalyzedParameter>(method.Parameters.Length - 1);
            var typeParameters = target.Target.GetTypeParameterNames().ToBuilder();
            foreach (var argument in target.TypeArguments)
                typeNames.Add((ITypeParameterSymbol)argument.Value, argument.Key);
            foreach (var parameter in method.TypeParameters)
            {
                var name = parameter.Name;
                while (typeParameters.Contains(name))
                    name += "_";
                typeParameters.Add(name);
                typeNames.Add(parameter, name);
            }
            TypeParameters = typeParameters.ToImmutable();
        }

        public ParsedTemplate Template { get; }
        public BoundTarget Target { get; }
        public IParameterSymbol Receiver { get; }
        public ImmutableArray<string> TypeParameters { get; }
        public ImmutableArray<AnalyzedParameter>.Builder Parameters { get; }
        public bool CanAwaitCallbacks =>
            calls.Values.Any(static call => call.IsCallback)
            && Parameters.All(static parameter => parameter.AwaitedTypeName is not null);

        public void AddFactory(ResolvedFactoryCall factory) =>
            calls.Add(factory.Call, (factory.Format(FormatType(Target.Other), FormatProperty), false));

        public void AddCallback(IInvocationOperation call, string expression) =>
            calls.Add(call, (expression, true));

        public string? WriteBody(bool awaitCallbacks) =>
            new TemplateBodyWriter(Template.Placeholders, Receiver, calls, awaitCallbacks).Write(Template.Body);

        public ITypeSymbol BindType(ITypeSymbol type) =>
            Template.Placeholders.IsSelf(type) ? Target.Self : Template.Placeholders.IsOther(type) ? Target.Other : type;

        public AnalyzedParameter DescribeParameter(string name, ITypeSymbol type, ITypeSymbol? awaitedType) =>
            new(name, FormatType(type), awaitedType is null ? null : FormatType(awaitedType),
                FormatType(type, true), awaitedType is null ? null : FormatType(awaitedType, true));

        public string FormatProperty(IPropertySymbol property)
        {
            var suppression = property.NullableAnnotation == NullableAnnotation.Annotated
                && Target.GetPropertyType(property).NullableAnnotation == NullableAnnotation.NotAnnotated ? "!" : "";
            return $"{TypeNames.Escape(Receiver.Name)}.{TypeNames.Escape(property.Name)}{suppression}";
        }

        public string FormatType(ITypeSymbol type, bool forSignature = false) =>
            TypeNames.Format(type, typeNames, forSignature);
    }

    private static bool HasSuccessProperty(AnalysisContext context, INamedTypeSymbol self)
    {
        for (INamedTypeSymbol? type = self; type is not null; type = type.BaseType)
        {
            var members = type.GetMembers("IsSuccess");
            if (!members.IsEmpty)
            {
                return members.Length == 1 && members[0] is IPropertySymbol { IsStatic: false, IsIndexer: false,
                    Type.SpecialType: SpecialType.System_Boolean, GetMethod: { } getter }
                    && context.Compilation.IsSymbolAccessibleWithin(getter, context.Extension.DestinationSymbol, self);
            }
        }

        return false;
    }

    private static bool UsesReceiver(IInvocationOperation call, IParameterSymbol receiver) =>
        call.Arguments.Length == 1 && call.Arguments[0].Value is IParameterReferenceOperation parameter
        && SymbolEqualityComparer.Default.Equals(parameter.Parameter, receiver);

    private static (ImmutableArray<IPropertySymbol> Success, ImmutableArray<IPropertySymbol> Failure)
        ReadProperties(AnalysisContext context, INamedTypeSymbol self)
    {
        var successProperties = new List<(IPropertySymbol Property, int? Index)>();
        var failureProperties = new List<(IPropertySymbol Property, int? Index)>();
        var shadowedNames = new HashSet<string>(System.StringComparer.Ordinal);
        for (INamedTypeSymbol? type = self; type is not null; type = type.BaseType)
        {
            var members = type.GetMembers();
            foreach (var property in members.OfType<IPropertySymbol>())
            {
                if (!shadowedNames.Contains(property.Name)
                    && property is { IsStatic: false, IsIndexer: false, GetMethod: { } getter }
                    && ResultApiAttributes.Find(property, ResultApiAttributes.CallbackArgument) is { } attribute
                    && attribute.ConstructorArguments.Length is 1 or 2
                    && attribute.ConstructorArguments[0].Value is bool success
                    && context.Compilation.IsSymbolAccessibleWithin(getter, context.Extension.DestinationSymbol, self))
                {
                    var arguments = attribute.ConstructorArguments;
                    int? index = arguments.Length == 2 && arguments[1].Value is int value ? value : null;
                    (success ? successProperties : failureProperties).Add((property, index));
                }
            }
            foreach (var member in members)
                shadowedNames.Add(member.Name);
        }

        return (OrderArguments(successProperties), OrderArguments(failureProperties));

        static ImmutableArray<IPropertySymbol> OrderArguments(List<(IPropertySymbol Property, int? Index)> properties) =>
            properties.OrderBy(static item => item.Index is null)
                .ThenBy(static item => item.Index)
                .ThenBy(static item => item.Property.Name, System.StringComparer.Ordinal)
                .Select(static item => item.Property)
                .ToImmutableArray();
    }

    private static INamedTypeSymbol? GetDelegateType(
        Compilation compilation, ImmutableArray<ITypeSymbol> arguments, ITypeSymbol? returnType)
    {
        var name = returnType is null ? "System.Action" : "System.Func";
        var types = returnType is null ? arguments : arguments.Add(returnType);
        if (types.IsEmpty)
            return compilation.GetTypeByMetadataName(name);
        var definition = compilation.GetTypeByMetadataName(name + "`" + types.Length);
        return definition?.Construct(types, types.Select(static type => type.NullableAnnotation).ToImmutableArray());
    }

    private readonly struct AnalysisContext(
        DetectedExtension extension,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        public DetectedExtension Extension { get; } = extension;
        public Compilation Compilation { get; } = compilation;
        public CancellationToken CancellationToken { get; } = cancellationToken;
        public ImmutableArray<ITypeParameterSymbol> TypeVariables { get; } =
            compilation.GetTypeByMetadataName("System.Func`17")?.TypeParameters ?? default;
    }

    private readonly struct ParsedTemplate(
        TemplateMethod method,
        IOperation body,
        TemplatePlaceholders placeholders,
        ImmutableArray<IInvocationOperation> factories,
        ImmutableArray<IInvocationOperation> invocations,
        bool usesIsSuccess)
    {
        public TemplateMethod Method { get; } = method;
        public IOperation Body { get; } = body;
        public TemplatePlaceholders Placeholders { get; } = placeholders;
        public ImmutableArray<IInvocationOperation> Factories { get; } = factories;
        public ImmutableArray<IInvocationOperation> Invocations { get; } = invocations;
        public bool UsesIsSuccess { get; } = usesIsSuccess;
    }

    private sealed class BoundTarget(
        ResultTarget target,
        INamedTypeSymbol self,
        INamedTypeSymbol other,
        IReadOnlyDictionary<string, ITypeSymbol> typeArguments,
        AnalysisContext context)
    {
        private readonly (ImmutableArray<IPropertySymbol> Success, ImmutableArray<IPropertySymbol> Failure) properties =
            ReadProperties(context, self);
        private readonly AnalysisContext analysisContext = context;
        private readonly bool guaranteesNonNullBranchValues =
            self.ContainingNamespace.ToDisplayString() == "InteractionFlow.Core.ResultAPI"
            && self.Name == "Result" && self.Arity <= 2;

        public ResultTarget Target { get; } = target;
        public INamedTypeSymbol Self { get; } = self;
        public INamedTypeSymbol Other { get; } = other;
        public IReadOnlyDictionary<string, ITypeSymbol> TypeArguments { get; } = typeArguments;
        public bool HasSuccessCheck { get; } = HasSuccessProperty(context, self);

        public ImmutableArray<IPropertySymbol> GetProperties(bool forSuccess) =>
            forSuccess ? properties.Success : properties.Failure;

        public ITypeSymbol GetPropertyType(IPropertySymbol property) =>
            property.Type.WithNullableAnnotation(
                guaranteesNonNullBranchValues && property.NullableAnnotation == NullableAnnotation.Annotated
                    ? NullableAnnotation.NotAnnotated
                    : property.NullableAnnotation);

        public ImmutableArray<(IMethodSymbol Factory, ImmutableArray<IPropertySymbol> Arguments)> FindFactories(
            IMethodSymbol placeholder, bool forSuccess)
        {
            var arguments = GetProperties(forSuccess).Select(property => (property, GetPropertyType(property)))
                .ToImmutableArray();
            return ResultTarget.FindFactoryMatches(
                placeholder, Self, Other, arguments, analysisContext.Compilation, analysisContext.Extension.DestinationSymbol);
        }
    }

    private sealed class ResolvedFactoryCall(
        IInvocationOperation call,
        IMethodSymbol factory,
        ImmutableArray<IPropertySymbol> arguments)
    {
        public IInvocationOperation Call { get; } = call;
        public IMethodSymbol Factory { get; } = factory;
        public ImmutableArray<IPropertySymbol> Arguments { get; } = arguments;

        public string Format(string targetType, System.Func<IPropertySymbol, string> formatArgument)
        {
            var values = Factory.Parameters.Zip(Arguments,
                (parameter, argument) => $"{TypeNames.Escape(parameter.Name)}: {formatArgument(argument)}");
            return $"{targetType}.{TypeNames.Escape(Factory.Name)}({string.Join(", ", values)})";
        }
    }
}
