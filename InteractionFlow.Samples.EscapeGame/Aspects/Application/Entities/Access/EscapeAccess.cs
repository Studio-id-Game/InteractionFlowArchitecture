using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Access;

internal sealed partial class EscapeAccess : IGameplayAccess, IViewAccess, ILifetimeAccess
{
    private readonly object gate = new();
    private readonly Dictionary<long, SessionRuntime> sessions = new();
    private SessionRuntime? active;
    private GameControlInput? controlInput;
    private long nextSession;
    private bool shutdown;

    private SessionRuntime? FindActiveSession(long id)
        => !shutdown && active is { Retired: false } current && current.State.SessionId == id ? current : null;

    private static void StopSession(SessionRuntime session)
    {
        session.Retired = true;
        session.State = session.State with { Mode = ExecutionMode.Stopping, Stage = GameStage.Stopping,
            WaitingForInput = false, ControlRevision = session.State.ControlRevision + 1 };
        session.Input?.TrySetResult(GameCommand.Stop);
        session.Resume?.TrySetResult(false);
        session.Presentation?.TrySetResult(PresentationCompletion.Stopped);
    }

    private static GameUpdateResult Commit(SessionRuntime session, GameUpdateResult change)
    {
        var next = change.State ?? throw new InvalidOperationException("An update must return game state.");
        if (next.SessionId != session.State.SessionId || next.Event is { } e && e.Key.SessionId != next.SessionId)
            throw new InvalidOperationException("An update cannot change its session identity.");
        var previous = session.State;
        if (next.Version != previous.Version + 1)
            next = next with { Version = previous.Version + 1 };
        session.State = next;
        if (previous.Event?.Key != next.Event?.Key)
        {
            session.Presentation?.TrySetResult(PresentationCompletion.Stopped);
            session.Presentation = next.Event is null ? null : new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        if (previous.Mode != next.Mode)
        {
            if (next.Mode == ExecutionMode.Paused)
                session.Resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
            else
            {
                session.Resume?.TrySetResult(next.Mode == ExecutionMode.Active);
                session.Resume = null;
            }
        }
        if (next.Mode == ExecutionMode.Stopping)
            StopSession(session);
        return change with { State = session.State };
    }

    private sealed class SessionRuntime(long id)
    {
        internal GameState State = new(id);
        internal bool Published;
        internal bool Retired;
        internal TaskCompletionSource<GameCommand>? Input;
        internal CancellationToken InputToken;
        internal TaskCompletionSource<bool>? Resume;
        internal TaskCompletionSource<PresentationCompletion>? Presentation;
    }
}
