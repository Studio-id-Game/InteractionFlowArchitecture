using InteractionFlow.ResultAPIGenerator.Interactions.Rules;
using Microsoft.CodeAnalysis;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed partial class AnalysisInteraction
{
    private sealed class TemplatePlaceholders
    {
        private readonly INamedTypeSymbol owner;

        public TemplatePlaceholders(INamedTypeSymbol owner, INamedTypeSymbol self)
        {
            this.owner = owner;
            Self = self;
        }

        public INamedTypeSymbol Self { get; }

        public static TemplatePlaceholders? Find(INamedTypeSymbol template)
        {
            for (INamedTypeSymbol? type = template; type is not null; type = type.BaseType)
            {
                if (ResultApiAttributes.Find(type, ResultApiAttributes.Template) is not null
                    && FindType(type, "Self", 0) is { } self)
                {
                    return new TemplatePlaceholders(type, self);
                }
            }

            return null;
        }

        public bool IsSelf(ITypeSymbol? type) => SymbolEqualityComparer.Default.Equals(type, Self);

        public bool IsOther(ITypeSymbol? type) =>
            type is INamedTypeSymbol named && Matches(named, "Other", 0);

        public bool IsSuccessProperty(IPropertySymbol property) =>
            property.Name == "IsSuccess" && IsSelf(property.ContainingType)
            && !property.IsStatic && property.Type.SpecialType == SpecialType.System_Boolean;

        public bool TryGetCallback(ITypeSymbol type, out bool forSuccess, out ITypeSymbol? returnType)
        {
            forSuccess = false;
            returnType = null;
            if (type is not INamedTypeSymbol named)
                return false;

            if (Matches(named, "OnSuccessFunc", 1) || Matches(named, "OnSuccessAction", 0))
                forSuccess = true;
            else if (!Matches(named, "OnErrorFunc", 1) && !Matches(named, "OnErrorAction", 0))
                return false;

            if (named.Arity == 1)
                returnType = named.TypeArguments[0];
            return true;
        }

        private bool Matches(INamedTypeSymbol type, string name, int arity) =>
            type.Name == name && type.Arity == arity && SymbolEqualityComparer.Default.Equals(type.ContainingType, owner);

        private static INamedTypeSymbol? FindType(INamedTypeSymbol owner, string name, int arity)
        {
            var types = owner.GetTypeMembers(name, arity);
            return types.Length == 1 ? types[0] : null;
        }
    }
}
