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
        public const string ExternalPorts = nameof(ExternalPorts);
        public const string Externals = nameof(Externals);
        public const string Interactions = nameof(Interactions);
        public const string ProgramFlows = nameof(ProgramFlows);

        private static readonly ImmutableHashSet<string> all = GetAll().ToImmutableHashSet(stringComparer);
        private static readonly ConcurrentDictionary<string, ImmutableHashSet<string>> disallowsSourceLayer = new(stringComparer);


        private static HashSet<string> GetAll() => new(stringComparer)
        {
            Builders,
            Entities,
            ExternalPorts,
            Externals,
            Interactions,
            ProgramFlows,
        };

        private static ImmutableHashSet<string> Disallows(string layerName)
        {
            var disallows = GetAll();
            disallows.Remove(Entities);
            disallows.Remove(layerName);

            switch (layerName)
            {
                case Builders:

                    disallows.Remove(ExternalPorts);
                    disallows.Remove(Externals);
                    disallows.Remove(Interactions);
                    disallows.Remove(ProgramFlows);
                    break;

                case Entities:
                case ExternalPorts:

                    break;

                case Externals:

                    disallows.Remove(ExternalPorts);
                    break;

                case ProgramFlows:

                    disallows.Remove(Interactions);
                    break;

                case Interactions:

                    disallows.Remove(ExternalPorts);
                    break;

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
                case Builders:
                case Externals:
                    return false;
            }

            foreach (var item in allowedRoots)
            {
                if (stringComparer.Equals(target, item) ||
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
