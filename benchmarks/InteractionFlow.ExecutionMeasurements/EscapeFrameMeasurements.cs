using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Externals;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Entities.Contexts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.ExternalPorts;
using InteractionFlow.Samples.EscapeGame.Aspects.View.Interactions;
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
        var initial = session.Gameplay.BeginSession(session.SessionId, WorldRules.BeginSession).Presentation!.Value;
        if (!root.View.TryCompletePresentation(initial)
            || session.Gameplay.UpdateGameState(session.SessionId,
                (state, _, _, completed) => WorldRules.BeginActionSelection(state, completed)).Status != GameUpdateStatus.Applied)
            throw new InvalidOperationException("Failed to initialize measurement session.");

        using var frames = new VC.ViewContext(root, ContextInheritance.None);
        var window = new VE.WindowState();
        window.MarkOpened();
        frames.With(window).With(new VE.FrameState());
        var poll = new Polling();
        var draw = new Drawing();
        var frame = new PresentFrame(poll, draw, new GameInputSubmissionOperation(),
            new GameExceptionHandling(), new GameCancellationHandling());

        Program.Measure("Escape game state read", () => sink = root.View.ReadGameState());
        Program.Measure("Escape idle frame direct (two reads, local clock)", () =>
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
            var state = context.View.ReadGameState();
            var frame = context.GetRequired<VE.FrameState>();
            frame.SessionId = state.State.SessionId;
            frame.Presentation = state.Info.Event?.Key;
            frame.Command = null;
            frame.PauseRequested = frame.SkipRequested = frame.ShutdownRequested = false;
        }
    }

    private sealed class Drawing : Reaction, IWindowReaction
    {
        private readonly VE.PresentationClock clock = new();
        public override void ForceResetMemoryState() => clock.Reset();
        public ReactionEnd Open(InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts.IViewContext context) => GetEnd();
        public ReactionEnd Close(InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts.ILifetimeContext context, Result previous) => GetEnd(previous);
        public ReactionEnd DrawFrame(InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Contexts.IViewContext context)
        {
            context.GetRequired<VE.WindowState>().VerifyAccess();
            var state = context.View.ReadGameState();
            clock.Update(state, 0);
            sink = state;
            return GetEnd();
        }
    }
}
