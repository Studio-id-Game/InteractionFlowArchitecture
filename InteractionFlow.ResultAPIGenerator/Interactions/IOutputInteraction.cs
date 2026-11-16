using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

/// <summary>生成ソースと診断をコンパイラーへ渡します。</summary>
internal interface IOutputInteraction
{
    void AddSource(SourceProductionContext context, GeneratedSource source);
    void ReportDiagnostic(SourceProductionContext context, Diagnostic diagnostic);
}
