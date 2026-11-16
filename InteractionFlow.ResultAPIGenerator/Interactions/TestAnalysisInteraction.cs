using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed class TestAnalysisInteraction : IAnalysisInteraction
{
    public InteractionResult<AnalyzedExtension> Analyze(
        DetectedExtension extension,
        Compilation compilation,
        CancellationToken cancellationToken)
        => new(new AnalyzedExtension(extension.Definition.Destination, default, default));
}
