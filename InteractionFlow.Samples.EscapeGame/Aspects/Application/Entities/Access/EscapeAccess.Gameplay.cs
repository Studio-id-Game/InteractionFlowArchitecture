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

    public GameUpdateResult BeginSession(long sessionId, Func<GameState, GameInfo<GameChangeInfo>> initialize)
    {
        lock (gate)
        {
            if (shutdown || !sessions.TryGetValue(sessionId, out var session)
                || session.Mode == ExecutionMode.Stopping)
                return new(GameUpdateStatus.Stopped);
            // 公開済みで停止していないセッションは必ずactiveが指しています。
            if (active is { Mode: not ExecutionMode.Stopping })
                return new(GameUpdateStatus.Rejected);
            var change = initialize(session.State);
            if (!change.Info.Accepted) return new(GameUpdateStatus.Rejected);
            var result = session.Commit(change);
            active = session;
            return result;
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

    public GameInfo<ExecutionMode> ReadGameState(long sessionId)
    {
        lock (gate)
            return FindActiveSession(sessionId) is { } session
                ? new(session.State, session.Mode)
                : new(new(sessionId) { Stage = GameStage.Stopping }, ExecutionMode.Stopping);
    }

    public GameUpdateResult UpdateGameState(long sessionId,
        Func<GameState, ExecutionMode, PresentationKey?, bool, GameInfo<GameChangeInfo>> update,
        bool allowWhilePaused = false)
    {
        lock (gate)
        {
            var session = FindActiveSession(sessionId);
            if (session is null)
                return new(GameUpdateStatus.Stopped);
            if (!allowWhilePaused && session.Mode == ExecutionMode.Paused)
                return new(GameUpdateStatus.Deferred);
            // 更新関数は短い同期処理。外部I/Oやawaitをせず、不変の次状態を返します。
            return session.Update(update);
        }
    }

    public async ValueTask<GameCommand> ReadCommandAsync(long sessionId, CancellationToken token)
    {
        SessionRuntime session;
        Task<GameCommand> task;
        lock (gate)
        {
            token.ThrowIfCancellationRequested();
            session = FindActiveSession(sessionId)!;
            if (session is null) return GameCommand.Stop;
            task = session.BeginInputWait(token);
        }
        try { return await task.WaitAsync(token).ConfigureAwait(false); }
        finally
        {
            lock (gate)
                session.EndInputWait();
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
                if (session is null) return false;
                var pending = session.GetResumeTask();
                if (pending is null) return true;
                resume = pending;
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
            var pending = FindActiveSession(key.SessionId)?.GetPresentationTask(key);
            if (pending is null)
                return PresentationCompletion.Stopped;
            task = pending;
        }
        return await task.WaitAsync(token).ConfigureAwait(false);
    }
}
