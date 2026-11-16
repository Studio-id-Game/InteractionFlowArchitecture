using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>生成先と、その生成に使うテンプレートおよび待機可能型を表します。</summary>
internal sealed class ExtensionDefinition(
    ExtensionClass destination,
    INamedTypeSymbol template,
    AwaitableTypes? awaitables,
    string suffix)
{
    public ExtensionClass Destination { get; } = destination;
    public INamedTypeSymbol Template { get; } = template;
    public AwaitableTypes? Awaitables { get; } = awaitables;

    /// <summary>テンプレートメソッド名に生成先の接尾辞を適用します。</summary>
    public string GetMethodName(IMethodSymbol method) => method.Name + suffix;
}
