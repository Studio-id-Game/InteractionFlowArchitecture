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
    public (ImmutableArray<AnalyzedCandidate> Candidates, ImmutableArray<HandwrittenMethod> HandwrittenMethods,
        ImmutableArray<Diagnostic> Diagnostics) Analyze(
        ExtensionDefinition definition,
        ImmutableArray<ResultTarget> targets,
        INamedTypeSymbol destination,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (templates, analyzedTargets, handwrittenMethods, templateDiagnostics) = AnalyzeTemplatesAndTargets(
            definition,
            targets,
            destination,
            compilation,
            cancellationToken);

        if (templates.IsDefaultOrEmpty || analyzedTargets.IsDefaultOrEmpty)
            return (ImmutableArray<AnalyzedCandidate>.Empty, handwrittenMethods, templateDiagnostics);

        var candidates = ImmutableArray.CreateBuilder<AnalyzedCandidate>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        AddDiagnostics(templateDiagnostics);

        foreach (var (template, body) in templates)
        {
            foreach (var target in analyzedTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!template.CanApplyTo(target) || BindResultTypes(target) is not { } boundTypes)
                    continue;

                var (factories, factoryDiagnostics) = ResolveFactories(
                    template,
                    body,
                    target,
                    boundTypes,
                    destination,
                    compilation,
                    cancellationToken);
                AddDiagnostics(factoryDiagnostics);

                var (candidate, candidateDiagnostics) = factories.IsDefault
                    ? SkipCandidate()
                    : AnalyzeCallbackArguments(
                        definition,
                        template,
                        body,
                        target,
                        boundTypes,
                        factories,
                        destination,
                        compilation,
                        cancellationToken);
                AddDiagnostics(candidateDiagnostics);

                if (candidate is not null)
                    candidates.Add(candidate);
            }
        }

        return (candidates.ToImmutable(), handwrittenMethods, diagnostics.ToImmutable());

        void AddDiagnostics(ImmutableArray<Diagnostic> items)
        {
            if (!items.IsDefaultOrEmpty)
                diagnostics.AddRange(items);
        }
    }

    private static (ImmutableArray<(TemplateMethod Method, IOperation Body)> Templates,
        ImmutableArray<ResultTarget> Targets, ImmutableArray<HandwrittenMethod> HandwrittenMethods,
        ImmutableArray<Diagnostic> Diagnostics) AnalyzeTemplatesAndTargets(
        ExtensionDefinition definition,
        ImmutableArray<ResultTarget> targets,
        INamedTypeSymbol destination,
        Compilation compilation,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    private static (INamedTypeSymbol Self, INamedTypeSymbol Other,
        IReadOnlyDictionary<string, ITypeSymbol> TypeArguments)? BindResultTypes(ResultTarget target)
        => throw new NotImplementedException();

    /// <summary>成立しなければ Factories は default、ファクトリが不要なら空です。</summary>
    private static (ImmutableArray<(IInvocationOperation Call, IMethodSymbol Factory,
        ImmutableArray<IPropertySymbol> Arguments)> Factories, ImmutableArray<Diagnostic> Diagnostics) ResolveFactories(
        TemplateMethod template,
        IOperation body,
        ResultTarget target,
        (INamedTypeSymbol Self, INamedTypeSymbol Other,
            IReadOnlyDictionary<string, ITypeSymbol> TypeArguments) boundTypes,
        INamedTypeSymbol destination,
        Compilation compilation,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    private static (AnalyzedCandidate? Candidate, ImmutableArray<Diagnostic> Diagnostics) SkipCandidate()
        => throw new NotImplementedException();

    private static (AnalyzedCandidate? Candidate, ImmutableArray<Diagnostic> Diagnostics) AnalyzeCallbackArguments(
        ExtensionDefinition definition,
        TemplateMethod template,
        IOperation body,
        ResultTarget target,
        (INamedTypeSymbol Self, INamedTypeSymbol Other,
            IReadOnlyDictionary<string, ITypeSymbol> TypeArguments) boundTypes,
        ImmutableArray<(IInvocationOperation Call, IMethodSymbol Factory,
            ImmutableArray<IPropertySymbol> Arguments)> factories,
        INamedTypeSymbol destination,
        Compilation compilation,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
