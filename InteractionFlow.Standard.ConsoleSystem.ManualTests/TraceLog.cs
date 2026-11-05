using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace InteractionFlow.Standard.ConsoleSystem.ManualTests
{
    internal sealed class TraceLog
    {
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly ConcurrentQueue<string> entries = new();

        internal void Add(string message)
        {
            entries.Enqueue($"[{clock.Elapsed.TotalMilliseconds,10:F1} ms] {message}");
        }

        internal void PrintAndSave(string? path)
        {
            var snapshot = entries.ToArray();
            Console.WriteLine("--- event trace (timestamps are relative to this run) ---");
            foreach (var entry in snapshot) Console.WriteLine(entry);

            if (path is null) return;

            var fullPath = Path.GetFullPath(path);
            File.AppendAllLines(fullPath, snapshot);
            Console.WriteLine($"Trace appended to: {fullPath}");
        }
    }
}
