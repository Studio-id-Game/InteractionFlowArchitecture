using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using System;
using E = InteractionFlow.Samples.EscapeGame.Entities;
using EC = InteractionFlow.Samples.EscapeGame.Entities.Contexts;
using EP = InteractionFlow.Samples.EscapeGame.ExternalPorts;
using EF = InteractionFlow.Samples.EscapeGame.Externals;
using EI = InteractionFlow.Samples.EscapeGame.Interactions;

namespace InteractionFlow.ExecutionMeasurements;

internal static class EscapeFrameMeasurements
{
    private static object? sink;

    internal static void Run()
    {
        using var root = new EC.EscapeApplicationContext();
        using var session = new EC.GameplayContext(root, root.Gameplay.CreateSession(), ContextInheritance.Cancellation);
        var initial = session.Gameplay.BeginSession(session.SessionId).Presentation!.Value;
        root.View.Advance(initial, 0, 0);
        if (!root.View.TryComplete(initial)
            || session.Gameplay.EnterSelecting(session.SessionId) != E.MutationStatus.Applied)
            throw new InvalidOperationException("Failed to initialize measurement session.");

        using var frames = new EC.GameViewContext(root, ContextInheritance.None);
        var window = new E.WindowState();
        window.MarkOpened();
        frames.With(window).With(new E.FrameState());
        var poll = new Polling();
        var draw = new Drawing();
        var frame = new EI.PresentFrame(poll, draw, new EF.GameOperation(),
            new EF.GameExceptionHandling(), new EF.GameCancellationHandling());

        Program.Measure("Escape Display.Snapshot", () => sink = root.View.Snapshot());
        Program.Measure("Escape idle frame direct (two snapshots)", () =>
        {
            poll.Poll(frames);
            _ = frames.GetRequired<E.FrameState>();
            sink = draw.Draw(frames);
        });
        Program.Measure("Escape idle PresentFrame.ExecuteAsync", () => sink = frame.ExecuteAsync(frames).GetAwaiter().GetResult());
        GC.KeepAlive(sink);
    }

    private sealed class Polling : Operation, EP.IWindowOperation
    {
        public override void ForceResetMemoryState() { }
        public void Poll(EC.IGameViewContext context)
        {
            context.GetRequired<E.WindowState>().VerifyAccess();
            var state = context.View.Snapshot();
            var frame = context.GetRequired<E.FrameState>();
            frame.SessionId = state.SessionId;
            frame.Presentation = state.Presentation?.Key;
            frame.Command = null;
            frame.PauseRequested = frame.SkipRequested = frame.ShutdownRequested = false;
        }
    }

    private sealed class Drawing : Reaction, EP.IWindowReaction
    {
        public override void ForceResetMemoryState() { }
        public ReactionEnd Open(EC.IGameViewContext context) => GetEnd();
        public ReactionEnd Close(EC.ILifetimeContext context, Result previous) => GetEnd(previous);
        public ReactionEnd Draw(EC.IGameViewContext context)
        {
            context.GetRequired<E.WindowState>().VerifyAccess();
            sink = context.View.Snapshot();
            return GetEnd();
        }
    }
}
