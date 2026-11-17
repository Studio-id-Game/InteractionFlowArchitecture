using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using InteractionFlow.ResultAPIGenerator.Interactions.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed partial class AnalysisInteraction : IAnalysisInteraction
{
    public InteractionResult<AnalyzedExtension> Analyze(
        DetectedExtension extension,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var context = new AnalysisContext(extension, compilation, cancellationToken);
        var destination = extension.Definition.Destination;
        var templates = AnalyzeTemplates(context);
        if (templates.Value.IsEmpty || extension.Targets.IsDefaultOrEmpty)
            return new InteractionResult<AnalyzedExtension>(new AnalyzedExtension(destination, default, default),
                templates.Diagnostics);

        var candidates = ImmutableArray.CreateBuilder<AnalyzedCandidate>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        diagnostics.AddRange(templates.Diagnostics);

        var boundTargets = BindTargets(context);

        foreach (var template in templates.Value)
        {
            foreach (var boundTarget in boundTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!template.Method.CanApplyTo(boundTarget.Target))
                    continue;

                var factories = ResolveFactories(context, template, boundTarget);
                diagnostics.AddRange(factories.Diagnostics);
                if (factories.Value.IsDefault)
                    continue;

                var analyzed = AnalyzeCandidate(context, template, boundTarget, factories.Value);
                diagnostics.AddRange(analyzed.Diagnostics);
                if (analyzed.Value is { } candidate)
                    candidates.Add(candidate);
            }
        }

        var handwrittenMembers = extension.DestinationSymbol.GetMembers()
            .Where(static member => member is not IMethodSymbol or IMethodSymbol { MethodKind: MethodKind.Ordinary })
            .Select(MethodSignatures.DescribeHandwritten)
            .ToImmutableArray();
        return new InteractionResult<AnalyzedExtension>(
            new AnalyzedExtension(destination, candidates.ToImmutable(), handwrittenMembers),
            diagnostics.ToImmutable());
    }

    private static InteractionResult<ImmutableArray<ParsedTemplate>> AnalyzeTemplates(AnalysisContext context)
    {
        var templates = ImmutableArray.CreateBuilder<ParsedTemplate>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var definition = context.Extension.Definition;
        var placeholders = TemplatePlaceholders.Find(definition.Template);
        foreach (var method in definition.Template.GetMembers().OfType<IMethodSymbol>())
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (ResultApiAttributes.Find(method, ResultApiAttributes.Function) is not { } attribute)
                continue;

            var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken);
            var arguments = attribute.ConstructorArguments;
            var generatedName = definition.GetMethodName(method);
            if (placeholders is null || syntax is not MethodDeclarationSyntax { Body: { } } declaration
                || !method.IsStatic || method.Parameters.IsEmpty || !placeholders.IsSelf(method.Parameters[0].Type)
                || method.ReturnsByRef || method.ReturnsByRefReadonly
                || method.TypeParameters.Any(TypeNames.HasConstraints)
                || !TypeNames.IsIdentifier(generatedName)
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
                generatedName, body, placeholders, factories, invocations, operations.OfType<IPropertyReferenceOperation>()
                    .Any(property => placeholders.IsSuccessProperty(property.Property))));
        }

        return new InteractionResult<ImmutableArray<ParsedTemplate>>(templates.ToImmutable(), diagnostics.ToImmutable());
    }

    private static InteractionResult<AnalyzedCandidate?> AnalyzeCandidate(
        AnalysisContext context,
        ParsedTemplate template,
        BoundTarget boundTarget,
        ImmutableArray<ResolvedFactoryCall> factories)
    {
        var method = template.Method.Method;
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
        var awaitedReceiver = template.Method.AllowAwaitedReceiver
            ? awaitables?.Wrap(boundTarget.Self) : null;
        var configuredReceiver = template.Method.AllowAwaitedReceiver
            ? awaitables?.WrapConfiguredReceiver(boundTarget.Self) : null;
        var awaitedBody = bindings.CanAwaitCallbacks ? bindings.WriteBody(awaitCallbacks: true) : null;
        var xml = method.GetDocumentationCommentXml(cancellationToken: context.CancellationToken);
        var candidate = new AnalyzedCandidate(
            TypeNames.Escape(template.GeneratedName),
            bindings.DescribeParameter(bindings.ReceiverName, boundTarget.Self, awaitedReceiver),
            bindings.FormatType(resolvedReturn), awaitedResult is null ? null : bindings.FormatType(awaitedResult),
            bindings.TypeParameters, bindings.Parameters.MoveToImmutable(), body, awaitedBody, xml,
            returnTypeKey: bindings.FormatType(resolvedReturn, forSignature: true),
            awaitedReturnTypeKey: awaitedResult is null ? null : bindings.FormatType(awaitedResult, forSignature: true),
            templateId: template.Id,
            forwardsReceiver: boundTarget.HasSameResultType && !factories.IsEmpty,
            configuredReceiver: configuredReceiver is null ? null
                : bindings.DescribeParameter(bindings.ReceiverName, boundTarget.Self, configuredReceiver));
        return new InteractionResult<AnalyzedCandidate?>(candidate);

        InteractionResult<AnalyzedCandidate?> Invalid(string message) =>
            new(null, ImmutableArray.Create(GeneratorDiagnostics.Create(method.Locations.FirstOrDefault(), message)));
    }

    private static string? BindCallbacks(
        Compilation compilation, CandidateBindings bindings, AwaitableTypes? awaitables)
    {
        var template = bindings.Template;
        var boundTarget = bindings.Target;
        var receiver = bindings.Receiver;
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
            if (awaitables is not null && template.Method.AllowAwaitedCallbacks)
            {
                awaitedDelegate = GetDelegateType(compilation, argumentTypes, awaitables.Wrap(returnType));
            }

            var name = TypeNames.Escape(parameter.Name);
            bindings.Parameters.Add(bindings.DescribeParameter(name, delegateType, awaitedDelegate));
            var arguments = string.Join(", ", properties.Select(bindings.FormatProperty));
            var expression = $"{name}({arguments})";
            var awaitedExpression = awaitedDelegate is not null
                ? AwaitableTypeRules.FormatAwait(expression, awaitables?.UsesConfigureAwait(returnType) == true)
                : null;
            var callback = new TemplateCall(expression, awaitedExpression);
            foreach (var call in template.CallbackCalls[parameter])
            {
                if (call.TargetMethod.Name != "Invoke" || !UsesReceiver(call, receiver))
                    return $"The callback '{parameter.Name}' must be invoked with the template receiver.";
                bindings.AddCallback(call, callback);
            }
        }

        return null;
    }

    private sealed class CandidateBindings
    {
        private readonly Dictionary<ITypeParameterSymbol, string> typeNames = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<IInvocationOperation, TemplateCall> calls = new();

        public CandidateBindings(ParsedTemplate template, BoundTarget target)
        {
            Template = template;
            Target = target;
            var method = template.Method.Method;
            Receiver = method.Parameters[0];
            ReceiverName = TypeNames.Escape(Receiver.Name);
            Parameters = ImmutableArray.CreateBuilder<AnalyzedParameter>(method.Parameters.Length - 1);
            var names = target.Target.TypeParameterNames;
            var typeParameters = ImmutableArray.CreateBuilder<string>(names.Length + method.TypeParameters.Length);
            var usedNames = new HashSet<string>(method.Parameters.Select(static parameter => parameter.Name), StringComparer.Ordinal)
            {
                template.GeneratedName,
            };
            for (var index = 0; index < names.Length; index++)
                AddTypeParameter(target.TypeVariables[index], names[index]);
            foreach (var parameter in method.TypeParameters)
                AddTypeParameter(parameter, parameter.Name);
            TypeParameters = typeParameters.MoveToImmutable();

            void AddTypeParameter(ITypeParameterSymbol parameter, string name)
            {
                while (!usedNames.Add(name))
                    name += "_";
                typeParameters.Add(name);
                typeNames.Add(parameter, name);
            }
        }

        public ParsedTemplate Template { get; }
        public BoundTarget Target { get; }
        public IParameterSymbol Receiver { get; }
        public string ReceiverName { get; }
        public ImmutableArray<string> TypeParameters { get; }
        public ImmutableArray<AnalyzedParameter>.Builder Parameters { get; }
        public bool CanAwaitCallbacks =>
            calls.Values.Any(static call => call.AwaitedExpression is not null)
            && Parameters.All(static parameter => parameter.AwaitedTypeName is not null);

        public void AddFactory(ResolvedFactoryCall factory)
        {
            var resolved = factory.ForwardsReceiver
                ? new TemplateCall(ReceiverName, forwardsReceiver: true)
                : new TemplateCall(factory.Format(FormatType(Target.Other), FormatProperty));
            calls.Add(factory.Call, resolved);
        }

        public void AddCallback(IInvocationOperation call, TemplateCall callback) =>
            calls.Add(call, callback);

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
            return $"{ReceiverName}.{TypeNames.Escape(property.Name)}{suppression}";
        }

        public string FormatType(ITypeSymbol type, bool forSignature = false) =>
            TypeNames.Format(type, typeNames, forSignature);
    }

    private static bool UsesReceiver(IInvocationOperation call, IParameterSymbol receiver) =>
        call.Arguments.Length == 1 && call.Arguments[0].Value is IParameterReferenceOperation parameter
        && SymbolEqualityComparer.Default.Equals(parameter.Parameter, receiver);

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
        string generatedName,
        IOperation body,
        TemplatePlaceholders placeholders,
        ImmutableArray<IInvocationOperation> factories,
        ImmutableArray<IInvocationOperation> invocations,
        bool usesIsSuccess)
    {
        public TemplateMethod Method { get; } = method;
        public string GeneratedName { get; } = generatedName;
        public string? Id { get; } = method.Method.GetDocumentationCommentId();
        public IOperation Body { get; } = body;
        public TemplatePlaceholders Placeholders { get; } = placeholders;
        public ImmutableArray<IInvocationOperation> Factories { get; } = factories;
        public ILookup<IParameterSymbol, IInvocationOperation> CallbackCalls { get; } = invocations
            .Where(static call => call.Instance is IParameterReferenceOperation)
            .ToLookup<IInvocationOperation, IParameterSymbol>(
                static call => ((IParameterReferenceOperation)call.Instance!).Parameter, SymbolEqualityComparer.Default);
        public bool UsesIsSuccess { get; } = usesIsSuccess;
    }
}
