using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ConsoleSystem.ManualTests
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Console.WriteLine("InteractionFlow ConsoleSystem manual tests");
            Console.WriteLine($"{RuntimeInformation.OSDescription} | {RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($"InputRedirected={Console.IsInputRedirected}, OutputRedirected={Console.IsOutputRedirected}");

            try
            {
                if (args.Length != 0)
                {
                    if (args[0] is "--help" or "-h")
                    {
                        PrintHelp();
                        return 0;
                    }

                    return await ConsoleProbe.RunAsync(ProbeOptions.Parse(args));
                }

                if (Console.IsInputRedirected)
                {
                    Console.Error.WriteLine("Redirected input requires an explicit scenario (for example: line).");
                    PrintHelp();
                    return 2;
                }

                return await RunMenuAsync();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static async Task<int> RunMenuAsync()
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("1: Line input / Ctrl+C (100ms grace)");
                Console.WriteLine("2: Line input / Ctrl+C after Enter (3000ms grace)");
                Console.WriteLine("3: Token cancellation after 1500ms, without Ctrl+C binding");
                Console.WriteLine("4: Already canceled before reading");
                Console.WriteLine("5: Three consecutive line reads (new cancellation context each time)");
                Console.WriteLine("6: Key input / Ctrl+C (no echo)");
                Console.WriteLine("7: Output colors and restoration");
                Console.WriteLine("0: Exit (Ctrl+C at this menu also exits normally)");
                Console.Write("Select: ");

                var selection = Console.ReadLine();
                if (selection is null or "0") return 0;

                ProbeOptions? options = selection switch
                {
                    "1" => new(),
                    "2" => new() { GraceMilliseconds = 3000 },
                    "3" => new() { CancelAfterMilliseconds = 1500, BindControlC = false },
                    "4" => new() { CancelBeforeRead = true },
                    "5" => new() { ReadCount = 3 },
                    "6" => new() { Scenario = "key" },
                    "7" => new() { Scenario = "output" },
                    _ => null
                };

                if (options is null)
                {
                    Console.WriteLine("Unknown selection.");
                    continue;
                }

                await ConsoleProbe.RunAsync(options);
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage: dotnet run --project InteractionFlow.Standard.ConsoleSystem.ManualTests -- [scenario] [options]");
            Console.WriteLine("No arguments: interactive menu. Scenarios: line, key, output");
            Console.WriteLine("--grace-ms N          Line cancellation grace: 0..60000 (default 100)");
            Console.WriteLine("--cancel-after-ms N   Request token cancellation N ms after starting each read");
            Console.WriteLine("--cancel-before       Request cancellation before starting each read");
            Console.WriteLine("--no-bind             Do not map Ctrl+C to cancellation; Ctrl+C can terminate the process");
            Console.WriteLine("--reads N             Read 1..100 times, using a fresh context each time");
            Console.WriteLine("--observe-ms N        Keep binding after each result: 0..60000 (default 300)");
            Console.WriteLine("--log PATH            Append the run's event trace to a file after completion");
        }
    }
}
