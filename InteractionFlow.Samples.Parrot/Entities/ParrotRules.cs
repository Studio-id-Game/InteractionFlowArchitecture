using System;
using System.Linq;

namespace InteractionFlow.Samples.Parrot.Entities
{
    internal static class ParrotRules
    {
        public const string DefaultGreeting = "Hello! I'm Parrot, who are you?";

        public static SampleMode ParseSample(string text)
        {
            text = text.Trim();
            if (int.TryParse(text, out var index))
                return index is >= 0 and < 6 ? (SampleMode)(index + 1) : SampleMode.None;

            foreach (var mode in Enum.GetValues<SampleMode>())
            {
                if (mode != SampleMode.None && string.Equals(text, mode.ToString(), StringComparison.OrdinalIgnoreCase))
                    return mode;
            }
            return SampleMode.None;
        }

        public static string Reply(string text)
        {
            var excited = text.Contains("Parrot", StringComparison.Ordinal);
            return string.Join(" ", Enumerable.Repeat(text, excited ? 6 : 3)) + (excited ? "!!!!!!!!!!" : "!");
        }
    }
}
