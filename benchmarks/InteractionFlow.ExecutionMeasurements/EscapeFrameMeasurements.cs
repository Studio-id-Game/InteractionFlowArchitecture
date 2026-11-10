using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Application.Externals;
using InteractionFlow.Samples.EscapeGame.Externals;
using InteractionFlow.Samples.EscapeGame.View.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.View.Interactions;
using System;
using E = InteractionFlow.Samples.EscapeGame.Entities;
using EC = InteractionFlow.Samples.EscapeGame.Entities.Contexts;

namespace InteractionFlow.ExecutionMeasurements;

internal static class EscapeFrameMeasurements
{
    private static object? sink;

    internal static void Run()
    {
        using var root = new EC.EscapeApplicationContext();
        using var session = new EC.GameplayContext(root, root.Gameplay.AllocateSession(), ContextInheritance.Cancellation);
        var initial = session.Gameplay.BeginSession(session.SessionId).Presentation!.Value;
        root.View.AdvancePresentation(initial, 0, 0);
        if (!root.View.TryCompletePresentation(initial)
            || session.Gameplay.BeginActionSelection(session.SessionId) != E.GameUpdateStatus.Applied)
            throw new InvalidOperationException("Failed to initialize measurement session.");

        using var frames = new EC.GameViewContext(root, ContextInheritance.None);
        var window = new E.WindowState();
        window.MarkOpened();
        frames.With(window).With(new E.FrameState());
        var poll = new Polling();
        var draw = new Drawing();
        var frame = new PresentFrame(poll, draw, new GameInputOperation(),
            new GameExceptionHandling(), new GameCancellationHandling());

        Program.Measure("Escape Display.Snapshot", () => sink = root.View.CaptureSnapshot());
        Program.Measure("Escape idle frame direct (two snapshots)", () =>
        {
            poll.PollFrameInput(frames);
            _ = frames.GetRequired<E.FrameState>();
            sink = draw.DrawFrame(frames);
        });
        Program.Measure("Escape idle PresentFrame.ExecuteAsync", () => sink = frame.ExecuteAsync(frames).GetAwaiter().GetResult());
        GC.KeepAlive(sink);
    }

    private sealed class Polling : Operation, IWindowOperation
    {
        public override void ForceResetMemoryState() { }
        public void PollFrameInput(EC.IGameViewContext context)
        {
            context.GetRequired<E.WindowState>().VerifyAccess();
            var state = context.View.CaptureSnapshot();
            var frame = context.GetRequired<E.FrameState>();
            frame.SessionId = state.SessionId;
            frame.Presentation = state.Presentation?.Key;
            frame.Command = null;
            frame.PauseRequested = frame.SkipRequested = frame.ShutdownRequested = false;
        }
    }

    private sealed class Drawing : Reaction, IWindowReaction
    {
        public override void ForceResetMemoryState() { }
        public ReactionEnd Open(EC.IGameViewContext context) => GetEnd();
        public ReactionEnd Close(EC.ILifetimeContext context, Result previous) => GetEnd(previous);
        public ReactionEnd DrawFrame(EC.IGameViewContext context)
        {
            context.GetRequired<E.WindowState>().VerifyAccess();
            sink = context.View.CaptureSnapshot();
            return GetEnd();
        }
    }
}
