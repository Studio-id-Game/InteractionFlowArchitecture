namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>生成候補との照合に使う手書きメンバーです。</summary>
internal readonly record struct HandwrittenMember(
    string Name,
    string? SignatureKey = null,
    string? ReturnTypeKey = null,
    bool CanReplaceGeneratedMethod = false)
{
    public string Name { get; } = Name;
    public string? SignatureKey { get; } = SignatureKey;
    public string? ReturnTypeKey { get; } = ReturnTypeKey;
    public bool CanReplaceGeneratedMethod { get; } = CanReplaceGeneratedMethod;
    public bool MatchesOverload(string signatureKey) => SignatureKey is null || SignatureKey == signatureKey;

    public bool CanReplace(string returnTypeKey) => CanReplaceGeneratedMethod && ReturnTypeKey == returnTypeKey;
}
