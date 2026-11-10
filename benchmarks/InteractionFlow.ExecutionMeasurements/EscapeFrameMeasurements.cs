using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Interactions;
using InteractionFlow.Samples.EscapeGame.Externals;
using System;
using AC = InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Contexts;
using E = InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using GameplayContexts = InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities.Contexts;
using VC = InteractionFlow.Samples.EscapeGame.Aspects.View.Entities.Contexts;
using VE = InteractionFlow.Samples.EscapeGame.Aspects.View.Entities;

namespace InteractionFlow.ExecutionMeasurements;

internal static class EscapeFrameMeasurements
{
    private static object? sink;

    internal static void Run()
    {
        using var root = new AC.EscapeApplicationContext();
        using var session = new GameplayContexts.GameplayContext(root, root.Gameplay.AllocateSession(), ContextInheritance.Cancellation);
        var initial = session.Gameplay.BeginSession(session.SessionId).Presentation!.Value;
        root.View.AdvancePresentation(initial, 0, 0);
        if (!root.View.TryCompletePresentation(initial)
            || session.Gameplay.BeginActionSelection(session.SessionId) != GameUpdateStatus.Applied)
            throw new InvalidOperationException("Failed to initialize measurement session.");

        using var frames = new VC.ViewContext(root, ContextInheritance.None);
        var window = new VE.WindowState();
        window.MarkOpened();
        frames.With(window).With(new VE.FrameState());
        var poll = new Polling();
        var draw = new Drawing();
        var frame = new PresentFrame(poll, draw, new GameInputOperation(),
            new GameExceptionHandling(), new GameCancellationHandling());

        Program.Measure("Escape Display.Snapshot", () => sink = root.View.CaptureSnapshot());
        Program.Measure("Escape idle frame direct (two snapshots)", () =>
        {
            poll.PollFrameInput(frames);
            _ = frames.GetRequired<VE.FrameState>();
            sink = draw.DrawFrame(frames);
        });
        Program.Measure("Escape idle PresentFrame.ExecuteAsync", () => sink = frame.ExecuteAsync(frames).GetAwaiter().GetResult());
        GC.KeepAlive(sink);
    }

    private sealed class Polling : Operation, IWindowOperation
    {
        public override void ForceResetMemoryState() { }
        public void PollFrameInput(InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts.IViewContext context)
        {
            context.GetRequired<VE.WindowState>().VerifyAccess();
            var state = context.View.CaptureSnapshot();
            var frame = context.GetRequired<VE.FrameState>();
            frame.SessionId = state.SessionId;
            frame.Presentation = state.Presentation?.Key;
            frame.Command = null;
            frame.PauseRequested = frame.SkipRequested = frame.ShutdownRequested = false;
        }
    }

    private sealed class Drawing : Reaction, IWindowReaction
    {
        public override void ForceResetMemoryState() { }
        public ReactionEnd Open(InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts.IViewContext context) => GetEnd();
        public ReactionEnd Close(InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts.ILifetimeContext context, Result previous) => GetEnd(previous);
        public ReactionEnd DrawFrame(InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts.IViewContext context)
        {
            context.GetRequired<VE.WindowState>().VerifyAccess();
            sink = context.View.CaptureSnapshot();
            return GetEnd();
        }
    }
}
