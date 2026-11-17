using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions.Rules;

internal static class ResultApiAttributes
{
    private const string Namespace = "InteractionFlow.Core.ResultAPI.Entities.Attributes.";
    private const string Template = Namespace + "ResultAPITemplateAttribute";

    public const string Extension = Namespace + "ResultAPIExtensionAttribute";
    public const string Target = Namespace + "ResultAPITargetAttribute";
    public const string Function = Namespace + "ResultAPIFuncAttribute";
    public const string CallbackArgument = Namespace + "ResultAPICallbackArgumentAttribute";

    public static AttributeData? Find(ISymbol symbol, string metadataName) =>
        symbol.GetAttributes().FirstOrDefault(attribute => Is(attribute, metadataName));

    public static bool Is(AttributeData attribute, string metadataName) =>
        string.Equals(attribute.AttributeClass?.ToDisplayString(), metadataName, StringComparison.Ordinal);

    public static IEnumerable<INamedTypeSymbol> GetTemplateOwners(INamedTypeSymbol template)
    {
        for (INamedTypeSymbol? type = template; type is not null; type = type.BaseType)
        {
            if (Find(type, Template) is not null)
                yield return type;
        }
    }
}
