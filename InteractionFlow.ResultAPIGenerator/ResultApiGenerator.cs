using InteractionFlow.ResultAPIGenerator.Entities;
using InteractionFlow.ResultAPIGenerator.Interactions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace InteractionFlow.ResultAPIGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class ResultApiGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Initialize(
            context,
            new DetectionInteraction(),
            new AnalysisInteraction(),
            new GenerationInteraction(),
            new OutputInteraction());
    }

    internal static void Initialize(
        IncrementalGeneratorInitializationContext context,
        IDetectionInteraction detection,
        IAnalysisInteraction analysis,
        IGenerationInteraction generation,
        IOutputInteraction output)
    {
        var analyzedExtensions = context.SyntaxProvider.ForAttributeWithMetadataName(
            ResultApiAttributes.Extension,
            static (node, _) => node is ClassDeclarationSyntax,
            (attributeContext, cancellationToken) =>
            {
                var detected = detection.Detect(attributeContext, cancellationToken);
                if (detected.Value is not { } extension)
                    return (Extension: (AnalyzedExtension?)null, Location: Location.None, detected.Diagnostics);

                var analyzed = analysis.Analyze(
                    extension,
                    attributeContext.SemanticModel.Compilation,
                    cancellationToken);

                return (Extension: (AnalyzedExtension?)analyzed.Value,
                    Location: attributeContext.TargetNode.GetLocation(),
                    Diagnostics: detected.Diagnostics.AddRange(analyzed.Diagnostics));
            });

        var generationInputs = analyzedExtensions
            .Where(static result => result.Extension is not null)
            .Select(static (result, _) => (Extension: result.Extension!, result.Location));
        var generated = generationInputs.Select((input, cancellationToken) =>
            generation.Generate(input.Extension, input.Location, cancellationToken))
            .WithTrackingName("ResultApiGeneration");
        var sources = generated
            .Where(static result => result.Value.HasValue)
            .Select(static (result, _) => result.Value.GetValueOrDefault())
            .Combine(context.CompilationProvider)
            .Select(static (input, cancellationToken) =>
                GeneratedTypeShortener.Shorten(input.Left, input.Right, cancellationToken));

        context.RegisterSourceOutput(sources, output.AddSource);
        context.RegisterSourceOutput(analyzedExtensions.SelectMany(static (result, _) => result.Diagnostics), output.ReportDiagnostic);
        context.RegisterSourceOutput(generated.SelectMany(static (result, _) => result.Diagnostics), output.ReportDiagnostic);
    }
}
