using System;

namespace InteractionFlow.Standard.ConsoleSystem.ManualTests
{
    internal sealed record ProbeOptions
    {
        internal string Scenario { get; init; } = "line";
        internal int GraceMilliseconds { get; init; } = 100;
        internal int CancelAfterMilliseconds { get; init; } = -1;
        internal int ObserveMilliseconds { get; init; } = 300;
        internal int ReadCount { get; init; } = 1;
        internal bool BindControlC { get; init; } = true;
        internal bool CancelBeforeRead { get; init; }
        internal string? LogPath { get; init; }

        internal static ProbeOptions Parse(string[] args)
        {
            var options = new ProbeOptions { Scenario = args[0] };
            if (options.Scenario is not ("line" or "key" or "output"))
                throw new ArgumentException($"Unknown scenario: {options.Scenario}");

            for (var index = 1; index < args.Length; index++)
            {
                var option = args[index];
                if (options.Scenario == "output" && option != "--log")
                    throw new ArgumentException("The output scenario only accepts --log.");

                switch (option)
                {
                    case "--no-bind":
                        options = options with { BindControlC = false };
                        break;
                    case "--cancel-before":
                        options = options with { CancelBeforeRead = true };
                        break;
                    case "--grace-ms":
                        options = options with { GraceMilliseconds = ReadNumber(args, ref index, 0, 60000) };
                        break;
                    case "--cancel-after-ms":
                        options = options with { CancelAfterMilliseconds = ReadNumber(args, ref index, 0, 600000) };
                        break;
                    case "--observe-ms":
                        options = options with { ObserveMilliseconds = ReadNumber(args, ref index, 0, 60000) };
                        break;
                    case "--reads":
                        options = options with { ReadCount = ReadNumber(args, ref index, 1, 100) };
                        break;
                    case "--log":
                        if (++index == args.Length) throw new ArgumentException("--log needs a file path.");
                        options = options with { LogPath = args[index] };
                        break;
                    default:
                        throw new ArgumentException($"Unknown option: {option}");
                }
            }

            return options;
        }

        private static int ReadNumber(string[] args, ref int index, int minimum, int maximum)
        {
            var option = args[index];
            if (++index == args.Length || !int.TryParse(args[index], out var value) ||
                value < minimum || value > maximum)
            {
                throw new ArgumentException($"{option} needs an integer from {minimum} to {maximum}.");
            }

            return value;
        }
    }
}
