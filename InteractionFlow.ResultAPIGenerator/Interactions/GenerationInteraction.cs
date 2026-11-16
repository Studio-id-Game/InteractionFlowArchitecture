using System;
using System.Collections.Immutable;
using System.Threading;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed class GenerationInteraction : IGenerationInteraction
{
    public (GeneratedSource? Source, ImmutableArray<Diagnostic> Diagnostics) Generate(
        ExtensionClass destination,
        ImmutableArray<AnalyzedCandidate> candidates,
        ImmutableArray<HandwrittenMethod> handwrittenMethods,
        Location diagnosticLocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var distinctCandidates = RemoveOverlappingCandidates(candidates, cancellationToken);
        var overloads = BuildOverloads(distinctCandidates, cancellationToken);
        var (methods, diagnostics) = CheckCollisions(
            overloads,
            handwrittenMethods,
            diagnosticLocation,
            cancellationToken);

        var source = methods.IsDefaultOrEmpty ? null : RenderSource(destination, methods, cancellationToken);
        return (source, diagnostics);
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
