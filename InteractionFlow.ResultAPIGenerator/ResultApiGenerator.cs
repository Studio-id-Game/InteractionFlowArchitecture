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
                var detected = detection.Detect(attributeContext, cancellationToken);
                if (detected.Value is not { } extension)
                {
                    return new InteractionResult<GeneratedSource?>(null, detected.Diagnostics);
                }

                var analyzed = analysis.Analyze(
                    extension,
                    attributeContext.SemanticModel.Compilation,
                    cancellationToken);

                var generated = generation.Generate(
                    analyzed.Value,
                    attributeContext.TargetNode.GetLocation(),
                    cancellationToken);

                return new InteractionResult<GeneratedSource?>(
                    generated.Value,
                    detected.Diagnostics.AddRange(analyzed.Diagnostics).AddRange(generated.Diagnostics));
            });

        var sources = results
            .Where(static result => result.Value.HasValue)
            .Select(static (result, _) => result.Value.GetValueOrDefault());
        var reportedDiagnostics = results.SelectMany(static (result, _) => result.Diagnostics);

        context.RegisterSourceOutput(sources, output.AddSource);
        context.RegisterSourceOutput(reportedDiagnostics, output.ReportDiagnostic);
    }
}
