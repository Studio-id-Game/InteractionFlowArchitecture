using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Access;

internal sealed partial class EscapeAccess
{
    public ViewSnapshot CaptureSnapshot()
    {
        lock (gate)
        {
            if (active is not { } session)
                return new(EscapeApplicationContext.NoSessionId, 0,
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

    public bool TrySubmitCommand(long sessionId, GameCommand command)
    {
        lock (gate)
        {
            var session = FindActiveSession(sessionId);
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
            var session = FindActiveSession(sessionId);
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

    // controlRevision は前フレームの値。ポーズ/再開の境界をまたいだ delta は捨てます。
    public PresentationSnapshot? AdvancePresentation(PresentationKey key, double elapsedSeconds, long controlRevision)
    {
        lock (gate)
        {
            var session = FindActiveSession(key.SessionId);
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

    public bool TryCompletePresentation(PresentationKey key)
    {
        lock (gate)
        {
            var session = FindActiveSession(key.SessionId);
            if (session is null || IsPaused(session) || session.Presentation is not { } presentation
                || presentation.Snapshot.Key != key || !presentation.Started
                || presentation.Snapshot.ElapsedSeconds < presentation.Snapshot.DurationSeconds)
                return false;
            return presentation.Completion.TrySetResult(PresentationCompletion.Completed);
        }
    }

    public bool TryRequestPresentationSkip(PresentationKey key)
    {
        lock (gate)
        {
            var session = FindActiveSession(key.SessionId);
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
