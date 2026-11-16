using System;
using System.Collections.Immutable;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed class DetectionInteraction : IDetectionInteraction
{
    public (ExtensionDefinition? Definition, ImmutableArray<ResultTarget> Targets,
        ImmutableArray<Diagnostic> Diagnostics) Detect(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var attribute = FindExtensionAttribute(context);
        return attribute is null
            ? (null, ImmutableArray<ResultTarget>.Empty, ImmutableArray<Diagnostic>.Empty)
            : ResolveSymbols(context, attribute, cancellationToken);
    }

    private static AttributeData? FindExtensionAttribute(GeneratorAttributeSyntaxContext context)
        => throw new NotImplementedException();

    private static (ExtensionDefinition? Definition, ImmutableArray<ResultTarget> Targets,
        ImmutableArray<Diagnostic> Diagnostics) ResolveSymbols(
        GeneratorAttributeSyntaxContext context,
        AttributeData extensionAttribute,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
