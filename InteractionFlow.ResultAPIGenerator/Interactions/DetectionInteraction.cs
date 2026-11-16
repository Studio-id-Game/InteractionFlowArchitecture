using System;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed class DetectionInteraction : IDetectionInteraction
{
    public InteractionResult<DetectedExtension?> Detect(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var attribute = FindExtensionAttribute(context);
        return attribute is null
            ? new InteractionResult<DetectedExtension?>(null)
            : ResolveSymbols(context, attribute, cancellationToken);
    }

    private static AttributeData? FindExtensionAttribute(GeneratorAttributeSyntaxContext context)
        => throw new NotImplementedException();

    private static InteractionResult<DetectedExtension?> ResolveSymbols(
        GeneratorAttributeSyntaxContext context,
        AttributeData extensionAttribute,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
