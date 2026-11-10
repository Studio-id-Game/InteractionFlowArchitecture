using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Access;

internal sealed partial class EscapeAccess
{
    public long AllocateSession()
    {
        lock (gate)
        {
            if (shutdown) throw new InvalidOperationException("The application is stopping.");
            var id = ++nextSession;
            sessions.Add(id, new SessionRuntime(id));
            return id;
        }
    }

    public GameUpdateResult BeginSession(long sessionId, Func<GameState, GameUpdateResult> initialize)
    {
        lock (gate)
        {
            if (shutdown || !sessions.TryGetValue(sessionId, out var session) || session.Retired)
                return new(GameUpdateStatus.Stopped);
            if (session.Published || active is { Retired: false })
                return new(GameUpdateStatus.Rejected);
            var change = initialize(session.State);
            if (change.Status != GameUpdateStatus.Applied) return change;
            change = session.Commit(change);
            active = session;
            session.Published = true;
            return change;
        }
    }

    public void EndSession(long sessionId)
    {
        lock (gate)
        {
            if (!sessions.Remove(sessionId, out var session)) return;
            session.StopSession();
            if (controlInput?.SessionId == sessionId) controlInput = null;
        }
    }

    public GameState ReadGameState(long sessionId)
    {
        lock (gate)
            return FindActiveSession(sessionId)?.State
                ?? new GameState(sessionId) { Mode = ExecutionMode.Stopping, Stage = GameStage.Stopping };
    }

    public GameUpdateResult UpdateGameState(long sessionId, Func<GameState, GameUpdateResult> update,
        bool allowWhilePaused = false)
    {
        lock (gate)
        {
            var session = FindActiveSession(sessionId);
            if (session is null || session.State.Mode == ExecutionMode.Stopping)
                return new(GameUpdateStatus.Stopped);
            if (!allowWhilePaused && session.State.Mode == ExecutionMode.Paused)
                return new(GameUpdateStatus.Deferred);
            // 更新関数は短い同期処理。外部I/Oやawaitをせず、不変の次状態を返します。
            return session.Commit(update(session.State));
        }
    }

    public async ValueTask<GameCommand> ReadCommandAsync(long sessionId, CancellationToken token)
    {
        SessionRuntime session;
        TaskCompletionSource<GameCommand> source;
        lock (gate)
        {
            token.ThrowIfCancellationRequested();
            session = FindActiveSession(sessionId)!;
            if (session is null) return GameCommand.Stop;
            if (session.Input is not null) throw new InvalidOperationException("Only one input wait is supported.");
            source = new(TaskCreationOptions.RunContinuationsAsynchronously);
            session.Input = source;
            session.InputToken = token;
            session.State = session.State with { WaitingForInput = true };
        }
        try { return await source.Task.WaitAsync(token).ConfigureAwait(false); }
        finally
        {
            lock (gate)
            {
                if (ReferenceEquals(session.Input, source))
                {
                    session.Input = null;
                    session.InputToken = default;
                    if (session.State.WaitingForInput)
                        session.State = session.State with { WaitingForInput = false };
                }
            }
        }
    }

    public bool TryReadControlInput(out GameControlInput input)
    {
        lock (gate)
        {
            if (controlInput is not { } pending) { input = default; return false; }
            input = pending;
            controlInput = null;
            return true;
        }
    }

    public async ValueTask<bool> WaitForActiveAsync(long sessionId, CancellationToken token)
    {
        while (true)
        {
            Task<bool> resume;
            lock (gate)
            {
                token.ThrowIfCancellationRequested();
                var session = FindActiveSession(sessionId);
                if (session is null || session.State.Mode == ExecutionMode.Stopping) return false;
                if (session.State.Mode != ExecutionMode.Paused) return true;
                resume = session.Resume!.Task;
            }
            if (!await resume.WaitAsync(token).ConfigureAwait(false)) return false;
        }
    }

    public async ValueTask<PresentationCompletion> WaitForPresentationCompletionAsync(
        PresentationKey key, CancellationToken token)
    {
        Task<PresentationCompletion> task;
        lock (gate)
        {
            token.ThrowIfCancellationRequested();
            var session = FindActiveSession(key.SessionId);
            if (session?.State.Event?.Key != key || session.Presentation is null)
                return PresentationCompletion.Stopped;
            task = session.Presentation.Task;
        }
        return await task.WaitAsync(token).ConfigureAwait(false);
    }
}
