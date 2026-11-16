using System;
using System.Collections.Immutable;
using System.Linq;

namespace InteractionFlow.ResultAPIGenerator.Entities;

/// <summary>型とメンバーを解決し、生成に渡せる状態になった候補です。</summary>
internal sealed class AnalyzedCandidate(
    string methodName,
    AnalyzedParameter receiver,
    string returnTypeName,
    string? awaitedReturnTypeName,
    ImmutableArray<string> typeParameterNames,
    ImmutableArray<AnalyzedParameter> parameters,
    string body,
    string? awaitedCallbacksBody,
    string? xmlDocumentation,
    string? returnTypeKey = null,
    string? awaitedReturnTypeKey = null,
    string? templateId = null,
    bool forwardsReceiver = false) : IEquatable<AnalyzedCandidate>
{
    public string MethodName { get; } = methodName;
    public AnalyzedParameter Receiver { get; } = receiver;
    public string ReturnTypeName { get; } = returnTypeName;
    public string? AwaitedReturnTypeName { get; } = awaitedReturnTypeName;
    public string ReturnTypeKey { get; } = returnTypeKey ?? returnTypeName;
    public string? AwaitedReturnTypeKey { get; } = awaitedReturnTypeKey ?? awaitedReturnTypeName;

    public ImmutableArray<string> TypeParameterNames { get; } =
        typeParameterNames.IsDefault ? ImmutableArray<string>.Empty : typeParameterNames;

    public ImmutableArray<AnalyzedParameter> Parameters { get; } =
        parameters.IsDefault ? ImmutableArray<AnalyzedParameter>.Empty : parameters;

    /// <summary>コールバックをそのまま呼び出す場合の本体です。</summary>
    public string Body { get; } = body;

    /// <summary>コールバックを待機する場合の本体です。該当する候補がなければ null です。</summary>
    public string? AwaitedCallbacksBody { get; } = awaitedCallbacksBody;

    public string? XmlDocumentation { get; } = xmlDocumentation;

    public string? TemplateId { get; } = templateId;
    public bool ForwardsReceiver { get; } = forwardsReceiver;

    public bool IsForwardingVariantOf(AnalyzedCandidate other) =>
        ForwardsReceiver && TemplateId is not null && TemplateId == other.TemplateId;

    private (string, AnalyzedParameter, string, string?, string, string?, string, string?, string?, string?, bool) ScalarValues =>
        (MethodName, Receiver, ReturnTypeName, AwaitedReturnTypeName, ReturnTypeKey, AwaitedReturnTypeKey,
            Body, AwaitedCallbacksBody, XmlDocumentation, TemplateId, ForwardsReceiver);

    public bool Equals(AnalyzedCandidate? other) =>
        ReferenceEquals(this, other) ||
        (other is not null && ScalarValues.Equals(other.ScalarValues)
            && TypeParameterNames.SequenceEqual(other.TypeParameterNames)
            && Parameters.SequenceEqual(other.Parameters));

    public override bool Equals(object? obj) => Equals(obj as AnalyzedCandidate);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = ScalarValues.GetHashCode();
            foreach (var name in TypeParameterNames)
                hash = hash * 31 + name.GetHashCode();
            foreach (var parameter in Parameters)
                hash = hash * 31 + parameter.GetHashCode();
            return hash;
        }
    }
}
