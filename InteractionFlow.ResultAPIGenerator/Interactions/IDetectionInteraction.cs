using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

/// <summary>生成先の属性からテンプレートと対象型を解決します。</summary>
internal interface IDetectionInteraction
{
    InteractionResult<DetectedExtension?> Detect(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken);
}
