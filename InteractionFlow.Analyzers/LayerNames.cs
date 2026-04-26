using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace InteractionFlow.Analyzers
{
    internal static class LayerNames
    {

        private static readonly StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;

        public const string Builders = nameof(Builders);
        public const string Entities = nameof(Entities);
        public const string Focuses = nameof(Focuses);
        public const string MultiFunctions = nameof(MultiFunctions);
        public const string MultiFunctionPorts = nameof(MultiFunctionPorts);
        public const string Interactions = nameof(Interactions);
        public const string OperationPorts = nameof(OperationPorts);
        public const string Operations = nameof(Operations);
        public const string ReactionPorts = nameof(ReactionPorts);
        public const string Reactions = nameof(Reactions);
        public const string StoragePorts = nameof(StoragePorts);
        public const string Storages = nameof(Storages);

        private static readonly ImmutableHashSet<string> all = GetAll().ToImmutableHashSet(stringComparer);
        private static readonly ConcurrentDictionary<string, ImmutableHashSet<string>> disallowsSourceLayer = new ConcurrentDictionary<string, ImmutableHashSet<string>>(stringComparer);


        private static HashSet<string> GetAll() => new HashSet<string>(stringComparer)
        {
            Builders,
            Entities,
            Focuses,
            Interactions,
            MultiFunctions,
            MultiFunctionPorts,
            Operations,
            OperationPorts,
            Reactions,
            ReactionPorts,
            Storages,
            StoragePorts
        };

        private static ImmutableHashSet<string> Disallows(string layerName)
        {
            var disallows = GetAll();
            disallows.Remove(Entities);
            disallows.Remove(layerName);

            switch (layerName)
            {
                case Builders:

                    disallows.Remove(Focuses);
                    disallows.Remove(MultiFunctions);
                    disallows.Remove(MultiFunctionPorts);
                    disallows.Remove(Operations);
                    disallows.Remove(OperationPorts);
                    disallows.Remove(Reactions);
                    disallows.Remove(ReactionPorts);
                    disallows.Remove(Storages);
                    disallows.Remove(StoragePorts);
                    break;

                case Focuses:

                    disallows.Remove(Interactions);
                    break;

                case Interactions:

                    disallows.Remove(OperationPorts);
                    disallows.Remove(ReactionPorts);
                    disallows.Remove(StoragePorts);
                    break;

                case MultiFunctions:
                case MultiFunctionPorts:
                case Operations:
                case OperationPorts:
                case Reactions:
                case ReactionPorts:
                case Storages:
                case StoragePorts:

                    disallows.Remove(MultiFunctionPorts);
                    disallows.Remove(OperationPorts);
                    disallows.Remove(ReactionPorts);
                    disallows.Remove(StoragePorts);
                    break;

                case Entities:
                default:
                    break;
            }

            return disallows.ToImmutableHashSet(stringComparer);
        }

        private static bool CheckDisallowExternal(IEnumerable<string> allowedRoots, string sourceLayer, string target, string targetLayer)
        {
            if (!string.IsNullOrEmpty(targetLayer))
            {
                return false;
            }

            switch (sourceLayer)
            {
                case Operations:
                case Reactions:
                case Storages:

                    return false;
            }

            foreach (var item in allowedRoots)
            {
                if (target.Equals(item, StringComparison.OrdinalIgnoreCase) ||
                    target.StartsWith($"{item}.", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsDisallowReference(IEnumerable<string> allowedRoots, string source, string target, out string sourceShowName, out string targetShowName)
        {
            var sourcePath = source.Split('.');
            var targetPath = target.Split('.');
            var sourceLayer = sourcePath.FirstOrDefault(e => all.Contains(e));
            var targetLayer = targetPath.FirstOrDefault(e => all.Contains(e));
            var isOutsideLayer = string.IsNullOrEmpty(sourceLayer);

            targetShowName = string.IsNullOrEmpty(targetLayer) ? target : targetLayer;
            sourceShowName = isOutsideLayer ? source : sourceLayer;

            if (isOutsideLayer)
            {
                return false;
            }


            if (!disallowsSourceLayer.TryGetValue(sourceLayer, out var disallows))
            {
                disallows = Disallows(sourceLayer);
                disallowsSourceLayer[sourceLayer] = disallows;
            }

            return disallows.Contains(targetLayer) || CheckDisallowExternal(allowedRoots, sourceLayer, target, targetLayer);
        }
    }
}
