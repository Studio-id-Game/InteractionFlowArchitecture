using System;
using System.Collections.Immutable;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed class GenerationInteraction : IGenerationInteraction
{
    public InteractionResult<GeneratedSource?> Generate(
        AnalyzedExtension extension,
        Location diagnosticLocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var distinctCandidates = RemoveOverlappingCandidates(extension.Candidates, cancellationToken);
        var overloads = BuildOverloads(distinctCandidates, cancellationToken);
        var (methods, diagnostics) = CheckCollisions(
            overloads,
            extension.HandwrittenMethods,
            diagnosticLocation,
            cancellationToken);

        var source = methods.IsDefaultOrEmpty ? null : RenderSource(extension.Destination, methods, cancellationToken);
        return new InteractionResult<GeneratedSource?>(source, diagnostics);
    }

    private static ImmutableArray<AnalyzedCandidate> RemoveOverlappingCandidates(
        ImmutableArray<AnalyzedCandidate> candidates,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    private static ImmutableArray<ExpandedMethod> BuildOverloads(
        ImmutableArray<AnalyzedCandidate> candidates,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    private static (ImmutableArray<ExpandedMethod> Methods, ImmutableArray<Diagnostic> Diagnostics) CheckCollisions(
        ImmutableArray<ExpandedMethod> methods,
        ImmutableArray<HandwrittenMethod> handwrittenMethods,
        Location diagnosticLocation,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    private static GeneratedSource? RenderSource(
        ExtensionClass destination,
        ImmutableArray<ExpandedMethod> methods,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    private sealed class ExpandedMethod(
        string signatureKey,
        string returnTypeKey,
        string declaration,
        string body,
        string? xmlDocumentation)
    {
        public string SignatureKey { get; } = signatureKey;
        public string ReturnTypeKey { get; } = returnTypeKey;
        public string Declaration { get; } = declaration;
        public string Body { get; } = body;
        public string? XmlDocumentation { get; } = xmlDocumentation;
    }
}
