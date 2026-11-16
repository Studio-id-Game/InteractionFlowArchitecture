namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>生成候補との照合に使う手書きメソッドの情報です。</summary>
internal readonly record struct HandwrittenMethod(
    string SignatureKey,
    string ReturnTypeKey,
    bool IsPublic,
    bool IsExtensionMethod)
{
    public string SignatureKey { get; } = SignatureKey;
    public string ReturnTypeKey { get; } = ReturnTypeKey;
    public bool IsPublic { get; } = IsPublic;
    public bool IsExtensionMethod { get; } = IsExtensionMethod;
}
