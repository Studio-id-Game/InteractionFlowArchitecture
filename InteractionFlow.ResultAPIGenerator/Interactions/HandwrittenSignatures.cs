using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using InteractionFlow.ResultAPIGenerator.Entities;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal static class HandwrittenSignatures
{
    public static HandwrittenMember Describe(ISymbol member)
    {
        if (member is not IMethodSymbol method)
            return new HandwrittenMember(member.Name);

        var names = new Dictionary<ITypeParameterSymbol, string>(SymbolEqualityComparer.Default);
        foreach (var parameter in method.TypeParameters)
            names.Add(parameter, KeyName(parameter.Ordinal));

        var parameters = method.Parameters.Select(parameter =>
            (TypeNames.Format(parameter.Type, names, forSignature: true), parameter.RefKind != RefKind.None));
        return new HandwrittenMember(
            method.Name,
            CreateKey(method.Name, method.Arity, parameters),
            TypeNames.Format(method.ReturnType, names, forSignature: true),
            method.DeclaredAccessibility == Accessibility.Public && method.IsExtensionMethod
                && !method.ReturnsByRef && !method.ReturnsByRefReadonly && !method.TypeParameters.Any(TypeNames.HasConstraints));
    }

    public static string CreateKey(string name, int arity, IEnumerable<(string Type, bool ByRef)> parameters) =>
        $"{name.TrimStart('@')}`{arity}({string.Join(",", parameters.Select(p => p.ByRef ? "&" + p.Type : p.Type))})";

    public static Dictionary<string, string> GetTypeParameters(ImmutableArray<string> names)
    {
        var keys = new Dictionary<string, string>(names.Length, System.StringComparer.Ordinal);
        for (var index = 0; index < names.Length; index++)
            keys.Add(names[index], KeyName(index));
        return keys;
    }

    private static string KeyName(int index) => $"__type{index}";
}
