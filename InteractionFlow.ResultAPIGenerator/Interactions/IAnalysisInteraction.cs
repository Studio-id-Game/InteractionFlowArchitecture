using System.Collections.Immutable;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

/// <summary>テンプレートを解析し、生成先ごとに成立した候補を返します。</summary>
internal interface IAnalysisInteraction
{
    (ImmutableArray<AnalyzedCandidate> Candidates, ImmutableArray<HandwrittenMethod> HandwrittenMethods,
        ImmutableArray<Diagnostic> Diagnostics) Analyze(
        ExtensionDefinition definition,
        ImmutableArray<ResultTarget> targets,
        INamedTypeSymbol destination,
        Compilation compilation,
        CancellationToken cancellationToken);
}
