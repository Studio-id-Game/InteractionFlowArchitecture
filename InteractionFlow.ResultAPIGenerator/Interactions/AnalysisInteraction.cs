using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;
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
        var (templates, analyzedTargets, handwrittenMethods, templateDiagnostics) = AnalyzeTemplatesAndTargets(context);

        if (templates.IsDefaultOrEmpty || analyzedTargets.IsDefaultOrEmpty)
        {
            return new InteractionResult<AnalyzedExtension>(
                new AnalyzedExtension(destination, ImmutableArray<AnalyzedCandidate>.Empty, handwrittenMethods),
                templateDiagnostics);
        }

        var candidates = ImmutableArray.CreateBuilder<AnalyzedCandidate>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        AddDiagnostics(templateDiagnostics);

        foreach (var template in templates)
        {
            foreach (var target in analyzedTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!template.Method.CanApplyTo(target) || BindResultTypes(target) is not { } boundTarget)
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

        return new InteractionResult<AnalyzedExtension>(
            new AnalyzedExtension(destination, candidates.ToImmutable(), handwrittenMethods),
            diagnostics.ToImmutable());

        void AddDiagnostics(ImmutableArray<Diagnostic> items)
        {
            if (!items.IsDefaultOrEmpty)
                diagnostics.AddRange(items);
        }
    }

    private static (ImmutableArray<ParsedTemplate> Templates,
        ImmutableArray<ResultTarget> Targets, ImmutableArray<HandwrittenMethod> HandwrittenMethods,
        ImmutableArray<Diagnostic> Diagnostics) AnalyzeTemplatesAndTargets(
        AnalysisContext context)
        => throw new NotImplementedException();

    private static BoundTarget? BindResultTypes(ResultTarget target)
        => throw new NotImplementedException();

    /// <summary>成立しなければ Factories は default、ファクトリが不要なら空です。</summary>
    private static (ImmutableArray<ResolvedFactoryCall> Factories,
        ImmutableArray<Diagnostic> Diagnostics) ResolveFactories(
        AnalysisContext context,
        ParsedTemplate template,
        BoundTarget boundTarget)
        => throw new NotImplementedException();

    private static (AnalyzedCandidate? Candidate, ImmutableArray<Diagnostic> Diagnostics) SkipCandidate()
        => throw new NotImplementedException();

    private static (AnalyzedCandidate? Candidate, ImmutableArray<Diagnostic> Diagnostics) AnalyzeCallbackArguments(
        AnalysisContext context,
        ParsedTemplate template,
        BoundTarget boundTarget,
        ImmutableArray<ResolvedFactoryCall> factories)
        => throw new NotImplementedException();

    private readonly struct AnalysisContext(
        DetectedExtension extension,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        public DetectedExtension Extension { get; } = extension;
        public Compilation Compilation { get; } = compilation;
        public CancellationToken CancellationToken { get; } = cancellationToken;
    }

    private readonly struct ParsedTemplate(TemplateMethod method, IOperation body)
    {
        public TemplateMethod Method { get; } = method;
        public IOperation Body { get; } = body;
    }

    private sealed class BoundTarget(
        ResultTarget target,
        INamedTypeSymbol self,
        INamedTypeSymbol other,
        IReadOnlyDictionary<string, ITypeSymbol> typeArguments)
    {
        public ResultTarget Target { get; } = target;
        public INamedTypeSymbol Self { get; } = self;
        public INamedTypeSymbol Other { get; } = other;
        public IReadOnlyDictionary<string, ITypeSymbol> TypeArguments { get; } = typeArguments;
    }

    private sealed class ResolvedFactoryCall(
        IInvocationOperation call,
        IMethodSymbol factory,
        ImmutableArray<IPropertySymbol> arguments)
    {
        public IInvocationOperation Call { get; } = call;
        public IMethodSymbol Factory { get; } = factory;
        public ImmutableArray<IPropertySymbol> Arguments { get; } = arguments;
    }
}
