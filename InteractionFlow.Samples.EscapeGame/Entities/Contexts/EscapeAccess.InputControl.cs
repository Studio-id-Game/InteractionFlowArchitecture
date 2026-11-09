using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

internal sealed partial class EscapeAccess
{
    private static bool StageAllowsInput(SessionRuntime session)
    {
        var state = session.Progress;
        return state.Stage == GameStage.Selecting && !state.Confirmed && state.Outcome == GameOutcome.Running
            || state.Stage == GameStage.Result && state.ResultInputEnabled && !state.RetryRequested;
    }

    private static bool CanAcceptInput(SessionRuntime session)
    {
        return !session.Progress.Retired && session.Mode == ExecutionMode.Active && StageAllowsInput(session)
            && session.Input is { Task.IsCompleted: false } && !session.InputToken.IsCancellationRequested;
    }

    public async ValueTask<GameCommand> ReadCommandAsync(long sessionId, CancellationToken token)
    {
        SessionRuntime session;
        TaskCompletionSource<GameCommand> source;
        lock (gate)
        {
            token.ThrowIfCancellationRequested();
            session = Current(sessionId)!;
            if (session is null)
                return GameCommand.Stop;
            if (session.Input is not null)
                throw new InvalidOperationException("Only one input wait is supported.");
            if (!StageAllowsInput(session))
                throw new InvalidOperationException("This stage does not accept game input.");
            source = new(TaskCreationOptions.RunContinuationsAsynchronously);
            session.Input = source;
            session.InputToken = token;
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
                }
            }
        }
    }

    public bool TrySubmitCommand(long sessionId, GameCommand command)
    {
        lock (gate)
        {
            var session = Current(sessionId);
            if (session is null || !CanAcceptInput(session) || command == GameCommand.Stop)
                return false;
            if (session.Progress.Stage == GameStage.Result && command != GameCommand.Retry)
                return false;
            if (session.Progress.Stage == GameStage.Selecting && command == GameCommand.Retry)
                return false;
            return session.Input!.TrySetResult(command);
        }
    }

    public bool TrySetPaused(long sessionId, bool paused)
    {
        lock (gate)
        {
            var session = Current(sessionId);
            if (session is null || session.Mode == ExecutionMode.Stopping
                || session.Progress.Stage is not (GameStage.Selecting
                    or GameStage.PlayerPresenting or GameStage.EnemyPresenting)
                || IsPaused(session) == paused)
                return false;

            session.ControlRevision++;
            session.Mode = paused ? ExecutionMode.Paused : ExecutionMode.Active;
            if (paused)
            {
                session.Resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            else
            {
                session.Resume?.TrySetResult(true);
                session.Resume = null;
            }

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
                var session = Current(sessionId);
                if (session is null || session.Mode == ExecutionMode.Stopping)
                    return false;
                if (!IsPaused(session))
                    return true;
                resume = session.Resume!.Task;
            }
            if (!await resume.WaitAsync(token).ConfigureAwait(false))
                return false;
        }
    }
}
