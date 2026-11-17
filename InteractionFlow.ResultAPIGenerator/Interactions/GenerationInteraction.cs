using InteractionFlow.ResultAPIGenerator.Entities;
using InteractionFlow.ResultAPIGenerator.Interactions.Rules;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace InteractionFlow.ResultAPIGenerator.Interactions;

internal sealed class GenerationInteraction : IGenerationInteraction
{
    public InteractionResult<GeneratedSource?> Generate(
        AnalyzedExtension extension,
        Location diagnosticLocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var distinctCandidates = RemoveOverlappingCandidates(extension.Candidates, cancellationToken);
        var overloads = BuildOverloads(distinctCandidates, cancellationToken);
        var (methods, diagnostics) = CheckCollisions(
            overloads,
            extension.HandwrittenMembers,
            diagnosticLocation,
            cancellationToken);

        GeneratedSource? source = methods.IsDefaultOrEmpty ? null : RenderSource(extension.Destination, methods, cancellationToken);
        return new InteractionResult<GeneratedSource?>(source, diagnostics);
    }

    private static ImmutableArray<AnalyzedCandidate> RemoveOverlappingCandidates(
        ImmutableArray<AnalyzedCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var retained = ImmutableArray.CreateBuilder<AnalyzedCandidate>();
        var byName = candidates.ToLookup(static candidate => candidate.MethodName, StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!byName[candidate.MethodName].Any(other => Covers(other, candidate)))
                retained.Add(candidate);
        }

        return retained.ToImmutable();
    }

    private static ImmutableArray<ExpandedMethod> BuildOverloads(
        ImmutableArray<AnalyzedCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var methods = ImmutableArray.CreateBuilder<ExpandedMethod>();
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (candidate.AwaitedReturnTypeName is null)
            {
                methods.Add(ExpandedMethod.Create(candidate, awaitCallbacks: false));
                continue;
            }

            var canAwaitCallbacks = candidate.AwaitedCallbacksBody is not null;
            if (canAwaitCallbacks)
                methods.Add(ExpandedMethod.Create(candidate, awaitCallbacks: true));
            if (candidate.Receiver.AwaitedTypeName is not null)
                AddReceiverOverloads(candidate.Receiver);
            if (candidate.ConfiguredReceiver is { } configuredReceiver)
                AddReceiverOverloads(configuredReceiver);

            void AddReceiverOverloads(AnalyzedParameter receiver)
            {
                methods.Add(ExpandedMethod.Create(candidate, awaitCallbacks: false, awaitedReceiver: receiver));
                if (canAwaitCallbacks)
                    methods.Add(ExpandedMethod.Create(candidate, awaitCallbacks: true, awaitedReceiver: receiver));
            }
        }

        return methods.ToImmutable();
    }

    private static (ImmutableArray<ExpandedMethod> Methods, ImmutableArray<Diagnostic> Diagnostics) CheckCollisions(
        ImmutableArray<ExpandedMethod> methods,
        ImmutableArray<HandwrittenMember> handwrittenMembers,
        Location diagnosticLocation,
        CancellationToken cancellationToken)
    {
        var accepted = ImmutableArray.CreateBuilder<ExpandedMethod>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var existing = handwrittenMembers.ToLookup(static member => member.Name, StringComparer.Ordinal);
        foreach (var group in methods.GroupBy(static method => method.SignatureKey, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var representative = group.First();
            var handwritten = existing[representative.Name]
                .Where(member => member.MatchesOverload(group.Key))
                .ToImmutableArray();
            if (!handwritten.IsEmpty)
            {
                if (handwritten.Length != 1 || group.Any(method => !handwritten[0].CanReplace(method.ReturnTypeKey)))
                {
                    diagnostics.Add(GeneratorDiagnostics.Create(diagnosticLocation,
                        $"A handwritten member conflicts with '{representative.Declaration}'."));
                }
                continue;
            }

            if (group.Any(method => method.Declaration != representative.Declaration || method.Body != representative.Body))
            {
                diagnostics.Add(GeneratorDiagnostics.Create(diagnosticLocation,
                    $"Generated methods conflict with '{representative.Declaration}'."));
                continue;
            }

            accepted.Add(representative);
        }

        return (accepted.ToImmutable(), diagnostics.ToImmutable());
    }

    private static GeneratedSource RenderSource(
        ExtensionClass destination,
        ImmutableArray<ExpandedMethod> methods,
        CancellationToken cancellationToken)
    {
        var source = new StringBuilder("// <auto-generated/>\n#nullable enable\n\n");
        if (destination.Namespace.Length != 0)
            source.Append("namespace ").Append(destination.Namespace).Append(";\n\n");
        source.Append($"//generate {methods.Length} methods\n");
        source.Append("static partial class ").Append(destination.DeclarationName).Append("\n{\n");
        foreach (var method in methods)
        {
            cancellationToken.ThrowIfCancellationRequested();
            method.AppendTo(source);
        }

        return new GeneratedSource(destination.GetSourceHintName(), source.Append("}\n").ToString());
    }

    private static bool Covers(AnalyzedCandidate general, AnalyzedCandidate specific)
    {
        if (general.Parameters.Length != specific.Parameters.Length
            || general.TypeParameterNames.Length != specific.TypeParameterNames.Length + 1)
        {
            return false;
        }

        var substitutions = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var removed = 0; removed < general.TypeParameterNames.Length; removed++)
        {
            substitutions.Clear();
            var index = 0;
            for (var slot = 0; slot < general.TypeParameterNames.Length; slot++)
            {
                if (slot != removed)
                    substitutions.Add(general.TypeParameterNames[slot], TypeNames.Escape(specific.TypeParameterNames[index++]));
            }

            foreach (var name in specific.TypeParameterNames)
            {
                substitutions[general.TypeParameterNames[removed]] = TypeNames.Escape(name);
                if (Equivalent())
                    return true;
            }
        }

        return false;

        bool Equivalent() =>
            SameParameter(general.Receiver, specific.Receiver)
            && SameConfiguredReceiver()
            && Same(general.ReturnTypeName, specific.ReturnTypeName)
            && Same(general.AwaitedReturnTypeName, specific.AwaitedReturnTypeName)
            && general.Parameters.Zip(specific.Parameters, SameParameter).All(static equal => equal)
            && (specific.IsForwardingVariantOf(general)
                || (Same(general.Body, specific.Body, isBody: true)
                    && Same(general.AwaitedCallbacksBody, specific.AwaitedCallbacksBody, isBody: true)));

        bool SameParameter(AnalyzedParameter first, AnalyzedParameter second) =>
            first.Name == second.Name && Same(first.TypeName, second.TypeName)
            && Same(first.AwaitedTypeName, second.AwaitedTypeName);

        bool SameConfiguredReceiver() => general.ConfiguredReceiver is { } receiver
            ? specific.ConfiguredReceiver is { } other && SameParameter(receiver, other)
            : specific.ConfiguredReceiver is null;

        bool Same(string? first, string? second, bool isBody = false) =>
            first is null ? second is null : TypeNames.ReplaceTypeParameters(first, substitutions, isBody) == second;
    }

    private sealed class ExpandedMethod(
        string name,
        string signatureKey,
        string returnTypeKey,
        string declaration,
        string body,
        string xmlDocumentation)
    {
        public string Name { get; } = name;
        public string SignatureKey { get; } = signatureKey;
        public string ReturnTypeKey { get; } = returnTypeKey;
        public string Declaration { get; } = declaration;
        public string Body { get; } = body;

        public static ExpandedMethod Create(
            AnalyzedCandidate candidate, bool awaitCallbacks, AnalyzedParameter? awaitedReceiver = null)
        {
            var typeParameters = MethodSignatures.CreateTypeParameterKeys(candidate.TypeParameterNames);
            var receiver = awaitedReceiver ?? candidate.Receiver;
            var awaitReceiver = awaitedReceiver.HasValue;
            var isAsync = awaitReceiver || awaitCallbacks;
            var returnType = isAsync ? candidate.AwaitedReturnTypeName! : candidate.ReturnTypeName;
            var returnTypeKey = isAsync ? candidate.AwaitedReturnTypeKey! : candidate.ReturnTypeKey;
            var parameterName = awaitReceiver ? GetAwaitedReceiverName(candidate) : candidate.Receiver.Name;
            var parameters = new List<string>();
            var parameterKeys = new List<(string, bool)>();
            AddParameter(receiver, awaitReceiver, receiverName: parameterName);
            foreach (var parameter in candidate.Parameters)
                AddParameter(parameter, awaitCallbacks);

            var generic = candidate.TypeParameterNames.IsEmpty
                ? ""
                : $"<{string.Join(", ", candidate.TypeParameterNames.Select(TypeNames.Escape))}>";
            var declaration = $"public static {(isAsync ? "async " : "")}{returnType} "
                + $"{candidate.MethodName}{generic}({string.Join(", ", parameters)})";
            var body = awaitCallbacks ? candidate.AwaitedCallbacksBody! : candidate.Body;
            if (awaitReceiver)
            {
                body = body.Insert(body.IndexOf('\n') + 1,
                    $"    var {candidate.Receiver.Name} = await {parameterName};\n");
            }

            var signature = MethodSignatures.CreateKey(candidate.MethodName, typeParameters.Count, parameterKeys);
            return new ExpandedMethod(candidate.MethodName.TrimStart('@'), signature, Key(returnTypeKey), declaration, body,
                ReadDocumentation(candidate, parameterName));

            string Key(string type) => TypeNames.ReplaceTypeParameters(type, typeParameters);

            void AddParameter(AnalyzedParameter parameter, bool awaited, string? receiverName = null)
            {
                var (type, key) = parameter.GetTypeNames(awaited);
                parameters.Add(receiverName is null ? $"{type} {parameter.Name}" : $"this {type} {receiverName}");
                parameterKeys.Add((Key(key), false));
            }
        }

        public void AppendTo(StringBuilder source)
        {
            foreach (var line in xmlDocumentation.Split('\n'))
                source.Append("    /// ").Append(line).Append('\n');
            source.Append("    ").Append(Declaration).Append('\n');
            foreach (var line in Body.TrimEnd('\n').Split('\n'))
                source.Append("    ").Append(line).Append('\n');
            source.Append('\n');
        }

        private static string GetAwaitedReceiverName(AnalyzedCandidate candidate)
        {
            var name = candidate.Receiver.Name.TrimStart('@') + "Task";
            while (candidate.Parameters.Any(parameter => parameter.Name.TrimStart('@') == name)
                || candidate.TypeParameterNames.Contains(name))
            {
                name += "_";
            }
            return name;
        }

        private static string ReadDocumentation(AnalyzedCandidate candidate, string receiverName)
        {
            const string fallback = "<summary>Executes the Result operation.</summary>";
            if (string.IsNullOrWhiteSpace(candidate.XmlDocumentation))
                return fallback;
            try
            {
                var xml = XElement.Parse(candidate.XmlDocumentation!);
                foreach (var element in xml.Elements("param"))
                {
                    if ((string?)element.Attribute("name") == candidate.Receiver.Name.TrimStart('@'))
                        element.SetAttributeValue("name", receiverName.TrimStart('@'));
                }
                var content = string.Join("\n", xml.Elements().Select(static element => element.ToString()).SelectMany(
                    static element => element.Replace("\r\n", "\n").Split('\n')).Select(static line => line.Trim()));
                return content.Length == 0 ? fallback : content;
            }
            catch (XmlException)
            {
                return fallback;
            }
        }
    }
}
