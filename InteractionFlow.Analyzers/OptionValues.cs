using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InteractionFlow.Analyzers
{
    internal class OptionValues
    {
        public OptionValues(AnalyzerConfigOptions options)
        {
            Enabled = GetEnabled(options);
            Mode = GetMode(options);
            AllowedRoots = GetAllowedRoots(options);
        }

        public bool Enabled { get; }

        public DiagnosticSeverity Mode { get; }

        public IEnumerable<string> AllowedRoots { get; }

        public static class Keys
        {
            public const string interactionflow_enabled = nameof(interactionflow_enabled);
            public const string interactionflow_mode = nameof(interactionflow_mode);
            public const string interactionflow_allowed_roots = nameof(interactionflow_allowed_roots);
        }

        public static bool GetEnabled(AnalyzerConfigOptions options)
        {
            if (options == null)
            {
                return false;
            }
            else if (!options.TryGetValue(Keys.interactionflow_enabled, out var text))
            {
                return false;
            }
            else if (!bool.TryParse(text, out var enabled))
            {
                return false;
            }
            else
            {
                return enabled;
            }
        }


        public static DiagnosticSeverity GetMode(AnalyzerConfigOptions options)
        {
            if (options == null)
            {
                return DiagnosticSeverity.Warning;
            }
            else if (!options.TryGetValue(Keys.interactionflow_mode, out var text))
            {
                return DiagnosticSeverity.Warning;
            }
            else if (!Enum.TryParse<DiagnosticSeverity>(text, out var severity))
            {
                return DiagnosticSeverity.Warning;
            }
            else
            {
                return severity;
            }
        }

        public static string[] GetAllowedRoots(AnalyzerConfigOptions options)
        {
            var roots = "System";
            if (options != null && options.TryGetValue(Keys.interactionflow_allowed_roots, out var value))
            {
                roots = $"{roots}, {value}";
            }

            return roots
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
