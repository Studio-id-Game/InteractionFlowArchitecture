namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>生成先の partial クラスを表します。</summary>
internal readonly record struct ExtensionClass(string Namespace, string DeclarationName, string HintName)
{
    public string Namespace { get; } = Namespace;
    public string DeclarationName { get; } = DeclarationName;
    public string HintName { get; } = HintName;
}
