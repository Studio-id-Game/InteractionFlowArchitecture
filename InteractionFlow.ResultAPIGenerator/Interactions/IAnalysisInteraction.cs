using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

/// <summary>テンプレートを解析し、生成先ごとに成立した候補を返します。</summary>
internal interface IAnalysisInteraction
{
    InteractionResult<AnalyzedExtension> Analyze(
        DetectedExtension extension,
        Compilation compilation,
        CancellationToken cancellationToken);
}
