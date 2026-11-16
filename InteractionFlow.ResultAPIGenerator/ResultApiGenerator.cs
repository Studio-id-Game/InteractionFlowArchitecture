using System.Collections.Immutable;
using InteractionFlow.ResultAPIGenerator.Entities;
using InteractionFlow.ResultAPIGenerator.Interactions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace InteractionFlow.ResultAPIGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class ResultApiGenerator : IIncrementalGenerator
{
    private const string ExtensionAttributeName =
        "InteractionFlow.Core.ResultAPI.Attributes.ResultAPIExtensionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Initialize(
            context,
            new TestDetectionInteraction(),
            new TestAnalysisInteraction(),
            new TestGenerationInteraction(),
            new OutputInteraction());
    }

    internal static void Initialize(
        IncrementalGeneratorInitializationContext context,
        IDetectionInteraction detection,
        IAnalysisInteraction analysis,
        IGenerationInteraction generation,
        IOutputInteraction output)
    {
        var results = context.SyntaxProvider.ForAttributeWithMetadataName(
            ExtensionAttributeName,
            static (node, _) => node is ClassDeclarationSyntax,
            (attributeContext, cancellationToken) =>
            {
                var (definition, targets, detectionDiagnostics) = detection.Detect(attributeContext, cancellationToken);
                var diagnostics = OrEmpty(detectionDiagnostics);
                if (definition is null)
                {
                    return (Source: (GeneratedSource?)null, Diagnostics: diagnostics);
                }

                var (candidates, handwrittenMethods, analysisDiagnostics) = analysis.Analyze(
                    definition,
                    OrEmpty(targets),
                    (INamedTypeSymbol)attributeContext.TargetSymbol,
                    attributeContext.SemanticModel.Compilation,
                    cancellationToken);

                var (source, generationDiagnostics) = generation.Generate(
                    definition.Destination,
                    OrEmpty(candidates),
                    OrEmpty(handwrittenMethods),
                    attributeContext.TargetNode.GetLocation(),
                    cancellationToken);

                return (Source: source,
                    Diagnostics: diagnostics.AddRange(OrEmpty(analysisDiagnostics))
                        .AddRange(OrEmpty(generationDiagnostics)));
            });

        var sources = results
            .Where(static result => result.Source.HasValue)
            .Select(static (result, _) => result.Source.GetValueOrDefault());
        var reportedDiagnostics = results.SelectMany(static (result, _) => result.Diagnostics);

        context.RegisterSourceOutput(sources, output.AddSource);
        context.RegisterSourceOutput(reportedDiagnostics, output.ReportDiagnostic);
    }

    private static ImmutableArray<T> OrEmpty<T>(ImmutableArray<T> values) =>
        values.IsDefault ? ImmutableArray<T>.Empty : values;
}
