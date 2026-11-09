using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.Entities.Contexts;
using InteractionFlow.Samples.Raylib.ExternalPorts;
using InteractionFlow.Samples.Raylib.Interactions;
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace InteractionFlow.ExecutionMeasurements;

internal static class Program
{
    private static object? sink;

    private static void Main()
    {
        Console.WriteLine($"{RuntimeInformation.FrameworkDescription}; {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"TieredCompilation={Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "default"}");
        Console.WriteLine("20000 warmup, 200000 operations x 3; current-thread allocation; no native drawing");

        using var context = new CounterFlowContext();
        var window = new WindowState();
        window.MarkOpened();
        context.With(new FrameState()).With(window);
        var reaction = new Drawing();
        var poll = new Polling();
        var errors = new Errors();
        var cancels = new Cancels();
        var frame = new PresentFrame(poll, reaction, new Commands(), errors, cancels);
        var success = reaction.Draw(context);
        var cachedTask = Task.FromResult(success);
        var fresh = new Probe(errors, cancels, () => Task.FromResult(success));
        var cached = new Probe(errors, cancels, () => cachedTask);
        var inline = new ValueProbe(errors, cancels, success);
        TaskCompletionSource<ReactionEnd>? pending = null;
        var suspended = new Probe(errors, cancels, () => pending!.Task);

        Measure("ReactionEnd success", () => sink = Drawing.Success());
        Measure("InteractionEnd factory", () => sink = EndFactory.Create(success));
        Measure("Task.FromResult", () => sink = Task.FromResult(success));
        Measure("async forwarding cached Task", () => sink = Forward(cachedTask));
        Measure("TCS and completion", () =>
        {
            var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            source.SetResult(true);
            sink = source;
        });
        Measure("registration only", () =>
        {
            using var registration = context.Cancellation.AddCancelableTask(Task.CompletedTask);
        });
        MeasureCaptureObject();
        Measure("minimal Interaction fresh body Task", () => sink = fresh.ExecuteAsync(context).GetAwaiter().GetResult());
        Measure("minimal Interaction cached body Task", () => sink = cached.ExecuteAsync(context).GetAwaiter().GetResult());
        Measure("minimal Interaction inline ValueTask body", () => sink = inline.ExecuteAsync(context).GetAwaiter().GetResult());
        Measure("controlled pending body roundtrip", () =>
        {
            // Inline completion deliberately isolates suspension from ThreadPool scheduling.
            pending = new TaskCompletionSource<ReactionEnd>();
            var execution = suspended.ExecuteAsync(context);
            if (execution.IsCompleted) throw new InvalidOperationException("Expected suspended execution.");
            pending.SetResult(success);
            if (!execution.IsCompleted) throw new InvalidOperationException("Unexpected asynchronous continuation.");
            sink = execution.GetAwaiter().GetResult();
        });
        Measure("direct frame body", () =>
        {
            poll.Poll(context);
            _ = context.GetRequired<FrameState>();
            sink = reaction.Draw(context);
        });
        Measure("PresentFrame.ExecuteAsync", () => sink = frame.ExecuteAsync(context).GetAwaiter().GetResult());
        EscapeFrameMeasurements.Run();
        GC.KeepAlive(sink);
    }

    private static async Task<ReactionEnd> Forward(Task<ReactionEnd> task)
        => await task.ConfigureAwait(false);

    private static void MeasureCaptureObject()
    {
        // Diagnostic only: generated type names/layout may change with compiler or implementation.
        foreach (var definition in typeof(Interaction<CounterFlowContext>).GetNestedTypes(BindingFlags.NonPublic))
        {
            if (!definition.Name.Contains("DisplayClass", StringComparison.Ordinal)) continue;
            var type = definition.ContainsGenericParameters
                ? definition.MakeGenericType(typeof(CounterFlowContext)) : definition;
            if (type.GetField("completion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is null)
                continue;
            var factory = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(type), typeof(object))).Compile();
            Measure("compiler capture object", () => sink = factory());
            return;
        }
        Console.WriteLine("compiler capture object: not found; skip implementation-specific diagnostic");
    }

    internal static void Measure(string name, Action action)
    {
        for (var i = 0; i < 20000; i++) action();
        for (var round = 1; round <= 3; round++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var before = GC.GetAllocatedBytesForCurrentThread();
            var start = Stopwatch.GetTimestamp();
            const int count = 200000;
            for (var i = 0; i < count; i++) action();
            var elapsed = Stopwatch.GetElapsedTime(start);
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Console.WriteLine($"{name}, {round}: {elapsed.TotalNanoseconds / count:F1} ns/op, {(double)bytes / count:F1} B/op");
        }
    }

    private sealed class Probe(Errors errors, Cancels cancels, Func<Task<ReactionEnd>> body)
        : Interaction<CounterFlowContext>(errors, cancels)
    {
        protected override ValueTask<ReactionEnd> ExecuteCoreAsync(CounterFlowContext context) => new(body());
    }

    private sealed class EndFactory : IInteraction<CounterFlowContext>
    {
        public ReadOnlyMemory<IDependencyNode> Dependency => ReadOnlyMemory<IDependencyNode>.Empty;
        internal static InteractionEnd Create(ReactionEnd end) => IInteraction<CounterFlowContext>.GetEnd(end);
        public Task<InteractionEnd> ExecuteAsync(CounterFlowContext context)
            => Task.FromResult(Create(Drawing.Success()));
    }

    private sealed class ValueProbe(Errors errors, Cancels cancels, ReactionEnd end)
        : Interaction<CounterFlowContext>(errors, cancels)
    {
        protected override ValueTask<ReactionEnd> ExecuteCoreAsync(CounterFlowContext context) => new(end);
    }

    private sealed class Polling : Operation, IWindowOperation
    {
        public override void ForceResetMemoryState() { }
        public void Poll(CounterFlowContext context)
        {
            context.GetRequired<WindowState>().VerifyAccess();
            var state = context.GetRequired<FrameState>();
            state.Command = null;
            state.CloseRequested = false;
            state.CancelRequested = false;
        }
    }

    private sealed class Drawing : Reaction, IWindowReaction
    {
        public override void ForceResetMemoryState() { }
        internal static ReactionEnd Success() => GetEnd();
        public ReactionEnd Open(CounterFlowContext context) => GetEnd();
        public ReactionEnd Close(CounterFlowContext context, Result result) => GetEnd(result);
        public ReactionEnd Draw(CounterFlowContext context)
        {
            context.GetRequired<WindowState>().VerifyAccess();
            context.Display.Snapshot(out _, out _, out _);
            return GetEnd();
        }
    }

    private sealed class Commands : Operation, ICounterOperation
    {
        public override void ForceResetMemoryState() { }
        public bool Submit(CounterFlowContext context, CounterCommand command) => context.Input.TrySubmit(command);
        public ValueTask<CounterCommand> ReadAsync(CounterFlowContext context)
            => context.Input.ReadCommandAsync(context.Cancellation.GetToken());
    }

    private sealed class Errors : ExceptionHandling
    {
        public override void ForceResetMemoryState() { }
        protected override ValueTask<ReactionEnd> HandleExceptionCoreAsync(IFlowContext context, Exception exception)
            => new(GetEnd(exception));
    }

    private sealed class Cancels : CancellationHandling
    {
        public override void ForceResetMemoryState() { }
        protected override ValueTask<ReactionEnd> AfterCancellationCoreAsync(
            IFlowContext context, OperationCanceledException exception, Result waitAndResetResult)
            => new(GetEnd(waitAndResetResult));
    }
}
