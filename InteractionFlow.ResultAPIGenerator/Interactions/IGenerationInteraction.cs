using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

/// <summary>生成候補を展開・照合し、生成先のソースを組み立てます。</summary>
internal interface IGenerationInteraction
{
    InteractionResult<GeneratedSource?> Generate(
        AnalyzedExtension extension,
        Location diagnosticLocation,
        CancellationToken cancellationToken);
}
