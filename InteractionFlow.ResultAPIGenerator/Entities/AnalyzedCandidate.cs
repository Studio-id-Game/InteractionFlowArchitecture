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
    private readonly ScalarValues scalarValues = new(
        methodName, receiver, returnTypeName, awaitedReturnTypeName,
        returnTypeKey ?? returnTypeName, awaitedReturnTypeKey ?? awaitedReturnTypeName,
        body, awaitedCallbacksBody, xmlDocumentation, templateId, forwardsReceiver);

    public string MethodName => scalarValues.MethodName;
    public AnalyzedParameter Receiver => scalarValues.Receiver;
    public string ReturnTypeName => scalarValues.ReturnTypeName;
    public string? AwaitedReturnTypeName => scalarValues.AwaitedReturnTypeName;
    public string ReturnTypeKey => scalarValues.ReturnTypeKey;
    public string? AwaitedReturnTypeKey => scalarValues.AwaitedReturnTypeKey;

    public ImmutableArray<string> TypeParameterNames { get; } =
        typeParameterNames.IsDefault ? ImmutableArray<string>.Empty : typeParameterNames;

    public ImmutableArray<AnalyzedParameter> Parameters { get; } =
        parameters.IsDefault ? ImmutableArray<AnalyzedParameter>.Empty : parameters;

    /// <summary>コールバックをそのまま呼び出す場合の本体です。</summary>
    public string Body => scalarValues.Body;

    /// <summary>コールバックを待機する場合の本体です。該当する候補がなければ null です。</summary>
    public string? AwaitedCallbacksBody => scalarValues.AwaitedCallbacksBody;

    public string? XmlDocumentation => scalarValues.XmlDocumentation;

    public string? TemplateId => scalarValues.TemplateId;
    public bool ForwardsReceiver => scalarValues.ForwardsReceiver;

    public bool IsForwardingVariantOf(AnalyzedCandidate other) =>
        ForwardsReceiver && TemplateId is not null && TemplateId == other.TemplateId;

    public bool Equals(AnalyzedCandidate? other) =>
        ReferenceEquals(this, other) ||
        (other is not null && scalarValues.Equals(other.scalarValues)
            && TypeParameterNames.SequenceEqual(other.TypeParameterNames)
            && Parameters.SequenceEqual(other.Parameters));

    public override bool Equals(object? obj) => Equals(obj as AnalyzedCandidate);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = scalarValues.GetHashCode();
            foreach (var name in TypeParameterNames)
                hash = hash * 31 + name.GetHashCode();
            foreach (var parameter in Parameters)
                hash = hash * 31 + parameter.GetHashCode();
            return hash;
        }
    }

    private sealed record ScalarValues(
        string MethodName,
        AnalyzedParameter Receiver,
        string ReturnTypeName,
        string? AwaitedReturnTypeName,
        string ReturnTypeKey,
        string? AwaitedReturnTypeKey,
        string Body,
        string? AwaitedCallbacksBody,
        string? XmlDocumentation,
        string? TemplateId,
        bool ForwardsReceiver)
    {
        public string MethodName { get; } = MethodName;
        public AnalyzedParameter Receiver { get; } = Receiver;
        public string ReturnTypeName { get; } = ReturnTypeName;
        public string? AwaitedReturnTypeName { get; } = AwaitedReturnTypeName;
        public string ReturnTypeKey { get; } = ReturnTypeKey;
        public string? AwaitedReturnTypeKey { get; } = AwaitedReturnTypeKey;
        public string Body { get; } = Body;
        public string? AwaitedCallbacksBody { get; } = AwaitedCallbacksBody;
        public string? XmlDocumentation { get; } = XmlDocumentation;
        public string? TemplateId { get; } = TemplateId;
        public bool ForwardsReceiver { get; } = ForwardsReceiver;
    }
}
