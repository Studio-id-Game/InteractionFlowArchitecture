namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>束縛済みの引数宣言を表します。</summary>
internal readonly record struct AnalyzedParameter(string Name, string TypeName, string? AwaitedTypeName = null)
{
    public string Name { get; } = Name;
    public string TypeName { get; } = TypeName;

    /// <summary>この引数を待機する版で使う型です。該当する型がなければ null です。</summary>
    public string? AwaitedTypeName { get; } = AwaitedTypeName;
}
