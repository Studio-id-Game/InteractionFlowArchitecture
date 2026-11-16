using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed class OutputInteraction : IOutputInteraction
{
    public void AddSource(SourceProductionContext context, GeneratedSource source)
        => context.AddSource(source.HintName, source.Source);

    public void ReportDiagnostic(SourceProductionContext context, Diagnostic diagnostic)
        => context.ReportDiagnostic(diagnostic);
}
