using System.Collections.Immutable;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed class TestAnalysisInteraction : IAnalysisInteraction
{
    public (ImmutableArray<AnalyzedCandidate> Candidates, ImmutableArray<HandwrittenMethod> HandwrittenMethods,
        ImmutableArray<Diagnostic> Diagnostics) Analyze(
        ExtensionDefinition definition,
        ImmutableArray<ResultTarget> targets,
        INamedTypeSymbol destination,
        Compilation compilation,
        CancellationToken cancellationToken) => default;
}
