using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class ResultApiGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
    }
}
