using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

internal sealed partial class EscapeAccess
{
    private static bool PresentationFinished(SessionRuntime session)
        => session.Presentation?.Completion.Task.IsCompletedSuccessfully == true
            && session.Presentation.Completion.Task.Result == PresentationCompletion.Completed;

    private MutationResult Present(
        SessionRuntime session, PresentationKind kind, GridPosition from, GridPosition to, double duration)
    {
        var key = new PresentationKey(session.Progress.Id, ++session.NextPresentation, session.Display.Version);
        session.Presentation = new(new(key, kind, from, to, duration, 0, false));
        return new(MutationStatus.Applied, key);
    }

    public async ValueTask<PresentationCompletion> WaitAsync(PresentationKey key, CancellationToken token)
    {
        Task<PresentationCompletion> task;
        lock (gate)
        {
            token.ThrowIfCancellationRequested();
            var session = Current(key.SessionId);
            if (session?.Presentation is not { } presentation || presentation.Snapshot.Key != key)
                return PresentationCompletion.Stopped;
            task = presentation.Completion.Task;
        }
        return await task.WaitAsync(token).ConfigureAwait(false);
    }

    // controlRevision は前フレームの値。ポーズ/再開の境界をまたいだ delta は捨てます。
    public PresentationSnapshot? Advance(PresentationKey key, double elapsedSeconds, long controlRevision)
    {
        lock (gate)
        {
            var session = Current(key.SessionId);
            if (session?.Presentation is not { } presentation || presentation.Snapshot.Key != key)
                return null;
            if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (IsPaused(session) || presentation.Completion.Task.IsCompleted)
                return presentation.Snapshot;
            if (!presentation.Started || session.ControlRevision != controlRevision)
            {
                presentation.Started = true;
                return presentation.Snapshot;
            }
            var value = presentation.Snapshot;
            var elapsed = value.SkipRequested ? value.DurationSeconds
                : Math.Min(value.DurationSeconds, value.ElapsedSeconds + elapsedSeconds);
            presentation.Snapshot = value with { ElapsedSeconds = elapsed };
            return presentation.Snapshot;
        }
    }

    public bool TryComplete(PresentationKey key)
    {
        lock (gate)
        {
            var session = Current(key.SessionId);
            if (session is null || IsPaused(session) || session.Presentation is not { } presentation
                || presentation.Snapshot.Key != key || !presentation.Started
                || presentation.Snapshot.ElapsedSeconds < presentation.Snapshot.DurationSeconds)
                return false;
            return presentation.Completion.TrySetResult(PresentationCompletion.Completed);
        }
    }

    public bool TrySkip(PresentationKey key)
    {
        lock (gate)
        {
            var session = Current(key.SessionId);
            if (session is null || IsPaused(session)
                || session.Progress.Stage is not (GameStage.PlayerPresenting or GameStage.EnemyPresenting)
                || session.Presentation is not { } presentation || presentation.Snapshot.Key != key
                || presentation.Completion.Task.IsCompleted)
                return false;
            presentation.Snapshot = presentation.Snapshot with { SkipRequested = true };
            return true;
        }
    }
}
