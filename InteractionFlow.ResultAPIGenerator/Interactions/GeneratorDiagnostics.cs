using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal static class GeneratorDiagnostics
{
    private static readonly DiagnosticDescriptor InvalidDeclaration = new(
        "IFRESULT001",
        "Invalid Result API declaration",
        "{0}",
        "ResultAPI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static Diagnostic Create(Location? location, string message) =>
        Diagnostic.Create(InvalidDeclaration, location, message);
}
