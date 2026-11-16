namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>コンパイラーへ渡すソースを表します。</summary>
internal readonly record struct GeneratedSource(string HintName, string Source)
{
    public string HintName { get; } = HintName;
    public string Source { get; } = Source;
}
