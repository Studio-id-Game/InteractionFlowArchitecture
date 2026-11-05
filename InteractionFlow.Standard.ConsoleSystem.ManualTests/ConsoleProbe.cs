using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts.SilentExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.Externals.Operations;
using InteractionFlow.Standard.ConsoleSystem.Externals.Reactions;
using InteractionFlow.Standard.ConsoleSystem.Externals.SilentExternals;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ConsoleSystem.ManualTests
{
    // 製品の Interaction ではなく、各 Port の挙動とタイミングを直接観測する検証ハーネス。
    internal static class ConsoleProbe
    {
        internal static async Task<int> RunAsync(ProbeOptions options)
        {
            var trace = new TraceLog();
            trace.Add($"ENV {RuntimeInformation.OSDescription} | {RuntimeInformation.FrameworkDescription} | " +
                $"inputRedirected={Console.IsInputRedirected}, outputRedirected={Console.IsOutputRedirected}");
            trace.Add($"START scenario={options.Scenario}, grace={options.GraceMilliseconds}ms, " +
                $"bind={options.BindControlC}, cancelBefore={options.CancelBeforeRead}, " +
                $"cancelAfter={options.CancelAfterMilliseconds}ms, reads={options.ReadCount}");

            var exitCode = 0;
            try
            {
                if (options.Scenario == "output")
                {
                    await CheckOutputAsync();
                }
                else
                {
                    for (var read = 1; read <= options.ReadCount; read++)
                    {
                        if (!await ReadOnceAsync(options, read, trace)) exitCode = 1;
                    }
                }
            }
            catch (Exception exception)
            {
                trace.Add($"HARNESS FAILURE: {exception}");
                exitCode = 1;
            }
            finally
            {
                trace.Add($"END exitCode={exitCode}");
                trace.PrintAndSave(options.LogPath);
            }

            return exitCode;
        }

        private static async Task<bool> ReadOnceAsync(ProbeOptions options, int number, TraceLog trace)
        {
            var context = new FlowContext();
            using var bindingLifetime = new ConsoleCancellationBinding();
            IConsoleCancellationBindingPort binding = bindingLifetime;
            using var timerLifetime = new CancellationTokenSource();
            using var tokenObserver = context.Cancellation.GetToken().Register(
                () => trace.Add($"read={number} TOKEN cancellation observed"));

            ConsoleCancelEventHandler eventObserver = (_, args) =>
                trace.Add($"read={number} CancelKeyPress observed: {args.SpecialKey}");

            Console.CancelKeyPress += eventObserver;
            Task timer = Task.CompletedTask;
            var success = true;
            try
            {
                if (options.BindControlC) binding.Bind(context);
                Console.WriteLine();
                Console.WriteLine($"Read {number}/{options.ReadCount}: scenario={options.Scenario}, " +
                    $"grace={options.GraceMilliseconds}ms, Ctrl+C binding={options.BindControlC}");
                Console.WriteLine("Line: type text and Enter. Key: press one key (no echo).");
                Console.WriteLine("After cancellation, press Enter if the read remains pending. Input is not flushed automatically.");
                if (!options.BindControlC)
                    Console.WriteLine("Ctrl+C is NOT handled here and may terminate this process.");
                if (options.CancelAfterMilliseconds >= 0)
                    Console.WriteLine($"Token cancellation is scheduled after {options.CancelAfterMilliseconds}ms.");
                Console.WriteLine($"After the result, observe late signals for {options.ObserveMilliseconds}ms.");

                if (options.CancelBeforeRead) context.Cancellation.Cancel();
                trace.Add($"read={number} CALL BEGIN");
                timer = CancelLaterAsync(options.CancelAfterMilliseconds, context, timerLifetime.Token, number, trace);
                var elapsed = Stopwatch.StartNew();

                try
                {
                    if (options.Scenario == "line")
                    {
                        IConsoleLineInputPort input = new ConsoleLineInput(
                            TimeSpan.FromMilliseconds(options.GraceMilliseconds));
                        var line = await input.ReadLineAsync(context);
                        ReportResult(trace, number, $"SUCCESS elapsed={elapsed.Elapsed.TotalMilliseconds:F1}ms, " +
                            $"line={JsonSerializer.Serialize(line)}, canceled={context.Cancellation.IsCancellationRequested}");
                    }
                    else
                    {
                        IConsoleKeyInputPort input = new ConsoleKeyInput();
                        var key = await input.ReadKeyAsync(context);
                        ReportResult(trace, number, $"SUCCESS elapsed={elapsed.Elapsed.TotalMilliseconds:F1}ms, " +
                            $"key={key.Key}, char={JsonSerializer.Serialize(key.KeyChar.ToString())}, " +
                            $"modifiers={key.Modifiers}, canceled={context.Cancellation.IsCancellationRequested}");
                    }
                }
                catch (OperationCanceledException)
                {
                    ReportResult(trace, number, $"CANCELED elapsed={elapsed.Elapsed.TotalMilliseconds:F1}ms, " +
                        $"canceled={context.Cancellation.IsCancellationRequested}");
                }
                catch (Exception exception)
                {
                    ReportResult(trace, number, $"FAILURE elapsed={elapsed.Elapsed.TotalMilliseconds:F1}ms: {exception}");
                    success = false;
                }

                Console.WriteLine("Call finished. Observing late signals; do not type the next input yet.");
                await Task.Delay(options.ObserveMilliseconds);
                trace.Add($"read={number} AFTER OBSERVATION canceled={context.Cancellation.IsCancellationRequested}");
            }
            finally
            {
                timerLifetime.Cancel();
                try
                {
                    await timer;
                }
                finally
                {
                    try
                    {
                        binding.Unbind();
                    }
                    finally
                    {
                        Console.CancelKeyPress -= eventObserver;
                        trace.Add($"read={number} UNBOUND (context was not reset)");
                    }
                }
            }

            return success;
        }

        private static void ReportResult(TraceLog trace, int read, string message)
        {
            trace.Add($"read={read} {message}");
            Console.WriteLine($"RESULT read={read} {message}");
        }

        private static async Task CancelLaterAsync(
            int delay, IFlowContext context, CancellationToken stop, int read, TraceLog trace)
        {
            if (delay < 0) return;

            try
            {
                await Task.Delay(delay, stop);
                trace.Add($"read={read} TIMER requesting cancellation");
                context.Cancellation.Cancel();
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
                // 検証終了時には、まだ発火していないタイマーを必ず回収する。
            }
        }

        private static async Task CheckOutputAsync()
        {
            var context = new FlowContext();
            IConsoleOutputPort output = new ConsoleOutput();
            await WriteLineAsync(output, context, "Default colors before styled output.");
            await WriteLineAsync(output, context, "Green foreground only.", new ConsoleTextStyle(ConsoleColor.Green));
            await WriteLineAsync(output, context, "Default colors restored.");
            await WriteLineAsync(output, context, "White on dark blue.",
                new ConsoleTextStyle(ConsoleColor.White, ConsoleColor.DarkBlue));
            await WriteLineAsync(output, context, "Default colors restored again.");
            Console.WriteLine("With redirected output, styles must not be applied.");
        }

        private static async Task WriteLineAsync(
            IConsoleOutputPort output, IFlowContext context, string text, ConsoleTextStyle style = default)
        {
            var end = await output.WriteLineAsync(context, text, style);
            if (end.HasException) throw end.Exception!;
        }
    }
}
