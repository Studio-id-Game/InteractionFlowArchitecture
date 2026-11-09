using System;
using System.Collections.Generic;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

internal sealed partial class EscapeAccess : IGameplayAccess, IGameViewAccess, IApplicationLifetimeAccess
{
    private readonly object gate = new();
    private readonly Dictionary<long, SessionRuntime> sessions = new();
    private SessionRuntime? active;
    private long nextSession;
    private bool shutdown;
    private static RoomDefinition Room => RoomDefinition.Default;

    public long AllocateSession()
    {
        lock (gate)
        {
            if (shutdown)
                throw new InvalidOperationException("The application is stopping.");
            var id = ++nextSession;
            sessions.Add(id, new SessionRuntime(id));
            return id;
        }
    }

    public bool ShutdownRequested
    {
        get
        {
            lock (gate)
                return shutdown;
        }
    }

    private SessionRuntime? Current(long id)
        => !shutdown && active is { } current && current.Progress.Id == id && !current.Progress.Retired
            ? current : null;

    private static bool IsPaused(SessionRuntime session) => session.Mode == ExecutionMode.Paused;
    private static MutationStatus CanMutate(SessionRuntime? session)
        => session is null || session.Mode == ExecutionMode.Stopping ? MutationStatus.Stopped
            : IsPaused(session) ? MutationStatus.Deferred : MutationStatus.Applied;

    public MutationResult BeginSession(long sessionId)
    {
        lock (gate)
        {
            if (shutdown || !sessions.TryGetValue(sessionId, out var session) || session.Progress.Retired)
                return new(MutationStatus.Stopped);
            if (session.Progress.Published || active is { Progress.Retired: false })
                return new(MutationStatus.Rejected);
            active = session;
            session.Progress.Published = true;
            session.Display.Set("Find the key, activate the switch, unlock the door");
            return Present(session, PresentationKind.Initial, session.Player.Position, session.Player.Position, 0);
        }
    }

    public GameProgress GetSessionProgress(long sessionId)
    {
        lock (gate)
        {
            var session = Current(sessionId);
            return session is null
                ? new(GameStage.Stopping, GameOutcome.Running, false, false, true, false)
                : new(session.Progress.Stage, session.Progress.Outcome, session.Progress.Confirmed,
                    session.Progress.RetryRequested, false, IsPaused(session));
        }
    }

    public GameSnapshot CaptureSnapshot()
    {
        lock (gate)
        {
            if (active is not { } session)
                return new(EscapeApplicationContext.ApplicationSessionId, 0,
                    Room, Room.PlayerStart, Room.PatrolAt(0), 0, false, false, false, false,
                    0, GameStage.Preparing, GameOutcome.Running,
                    shutdown ? ExecutionMode.Stopping : ExecutionMode.Active, 0, null,
                    "Preparing room", false, false, false, shutdown, null);
            var state = session.Progress;
            var accepting = CanAcceptInput(session);
            return new(state.Id, session.Display.Version, Room, session.Player.Position,
                Room.PatrolAt(session.Enemy.PatrolIndex), session.Enemy.PatrolIndex,
                session.Inventory.HasKey, session.Mechanisms.KeyCollected, session.Mechanisms.SwitchActivated,
                session.Mechanisms.DoorUnlocked, state.Turn, state.Stage, state.Outcome, session.Mode,
                session.ControlRevision, state.Selection, session.Display.Message, accepting,
                accepting && state.Selection.HasValue, state.RetryRequested, shutdown,
                session.Presentation?.Snapshot);
        }
    }

    public void EndSession(long sessionId)
    {
        lock (gate)
        {
            if (!sessions.TryGetValue(sessionId, out var session)) return;
            Stop(session);
            sessions.Remove(sessionId);
        }
    }

    public void RequestShutdown()
    {
        lock (gate)
        {
            if (shutdown) return;
            shutdown = true;
            foreach (var session in sessions.Values) Stop(session);
            sessions.Clear();
        }
    }

    private static void Stop(SessionRuntime session)
    {
        session.Progress.Retired = true;
        session.Progress.Stage = GameStage.Stopping;
        session.Mode = ExecutionMode.Stopping;
        session.ControlRevision++;
        session.Input?.TrySetResult(GameCommand.Stop);
        session.Resume?.TrySetResult(false);
        session.Presentation?.Completion.TrySetResult(PresentationCompletion.Stopped);
    }
}
