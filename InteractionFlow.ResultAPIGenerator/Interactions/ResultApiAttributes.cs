using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal static class ResultApiAttributes
{
    private const string Namespace = "InteractionFlow.Core.ResultAPI.Attributes.";

    public const string Extension = Namespace + "ResultAPIExtensionAttribute";
    public const string Template = Namespace + "ResultAPITemplateAttribute";
    public const string Target = Namespace + "ResultAPITargetAttribute";
    public const string Function = Namespace + "ResultAPIFuncAttribute";
    public const string CallbackArgument = Namespace + "ResultAPICallbackArgumentAttribute";

    public static AttributeData? Find(ISymbol symbol, string metadataName) =>
        symbol.GetAttributes().FirstOrDefault(attribute => Is(attribute, metadataName));

    public static bool Is(AttributeData attribute, string metadataName) =>
        string.Equals(attribute.AttributeClass?.ToDisplayString(), metadataName, StringComparison.Ordinal);
}
