using System;
using System.Collections.Immutable;
using System.Linq;
using InteractionFlow.ResultAPIGenerator.Entities;
using InteractionFlow.ResultAPIGenerator.Interactions.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed partial class AnalysisInteraction
{
    /// <summary>不成立なら Factories は default、テンプレートに Create 呼び出しがなければ空です。</summary>
    private static InteractionResult<ImmutableArray<ResolvedFactoryCall>> ResolveFactories(
        AnalysisContext context,
        ParsedTemplate template,
        BoundTarget boundTarget)
    {
        var factories = ImmutableArray.CreateBuilder<ResolvedFactoryCall>(template.Factories.Length);
        foreach (var call in template.Factories)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (!UsesReceiver(call, template.Method.Method.Parameters[0])
                || !call.TargetMethod.IsStatic
                || call.TargetMethod.Name is not ("CreateSuccess" or "CreateFailure"))
            {
                return Invalid("A factory placeholder must receive only the template receiver.");
            }

            if (boundTarget.HasSameResultType)
            {
                factories.Add(new ResolvedFactoryCall(call));
                continue;
            }

            var matches = boundTarget.FindFactories(call.TargetMethod.Name);
            if (matches.IsEmpty)
                return default;

            var best = matches.Length == 1
                ? matches[0]
                : matches.SingleOrDefault(candidate => matches.All(other =>
                    SymbolEqualityComparer.Default.Equals(candidate.Factory, other.Factory)
                    || IsBetterFactory(candidate.Factory, other.Factory, context.Compilation)));
            if (best.Factory is null)
                return Invalid($"Factory '{call.TargetMethod.Name}' is ambiguous for '{boundTarget.Other}'.");

            factories.Add(new ResolvedFactoryCall(call, best.Factory, best.Arguments));

            InteractionResult<ImmutableArray<ResolvedFactoryCall>> Invalid(string message) =>
                new(default, ImmutableArray.Create(GeneratorDiagnostics.Create(call.Syntax.GetLocation(), message)));
        }

        return new InteractionResult<ImmutableArray<ResolvedFactoryCall>>(factories.MoveToImmutable());
    }

    private static bool IsBetterFactory(IMethodSymbol candidate, IMethodSymbol other, Compilation compilation)
    {
        if (candidate.Parameters.Length != other.Parameters.Length)
            return false;

        var strictlyBetter = false;
        foreach (var parameter in candidate.Parameters)
        {
            var counterpart = other.Parameters.FirstOrDefault(item => item.Name == parameter.Name);
            if (counterpart is null)
                return false;

            var conversion = compilation.ClassifyCommonConversion(parameter.Type, counterpart.Type);
            if (conversion.IsIdentity)
                continue;
            if (!conversion.IsImplicit || !conversion.IsReference)
                return false;
            strictlyBetter = true;
        }

        return strictlyBetter;
    }

    private sealed class ResolvedFactoryCall(
        IInvocationOperation call,
        IMethodSymbol? factory = null,
        ImmutableArray<IPropertySymbol> arguments = default)
    {
        public IInvocationOperation Call { get; } = call;
        public bool ForwardsReceiver => factory is null;

        public string Format(string targetType, Func<IPropertySymbol, string> formatArgument)
        {
            var values = factory!.Parameters.Zip(arguments,
                (parameter, argument) => $"{TypeNames.Escape(parameter.Name)}: {formatArgument(argument)}");
            return $"{targetType}.{TypeNames.Escape(factory.Name)}({string.Join(", ", values)})";
        }
    }
}
