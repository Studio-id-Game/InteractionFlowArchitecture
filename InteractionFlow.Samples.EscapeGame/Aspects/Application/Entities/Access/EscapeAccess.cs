using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Gameplay.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Access;

internal sealed partial class EscapeAccess : IGameplayAccess, IViewAccess, ILifetimeAccess
{
    private readonly object gate = new();

    private readonly Dictionary<long, SessionRuntime> sessions = new();

    private SessionRuntime? active;

    private long nextSession;

    private bool shutdown;

    private static RoomDefinition Room => RoomDefinition.Default;

    public SessionProgress GetSessionProgress(long sessionId)
    {
        lock (gate)
        {
            var session = FindActiveSession(sessionId);
            return session is null
                ? new(GameStage.Stopping, GameOutcome.Running, false, false, true, false)
                : new(session.Progress.Stage, session.Progress.Outcome, session.Progress.Confirmed,
                    session.Progress.RetryRequested, false, IsPaused(session));
        }
    }

    private static void StopSession(SessionRuntime session)
    {
        session.Progress.Retired = true;
        session.Progress.Stage = GameStage.Stopping;
        session.Mode = ExecutionMode.Stopping;
        session.ControlRevision++;
        session.Input?.TrySetResult(GameCommand.Stop);
        session.Resume?.TrySetResult(false);
        session.Presentation?.Completion.TrySetResult(PresentationCompletion.Stopped);
    }

    private SessionRuntime? FindActiveSession(long id)
        => !shutdown && active is { } current && current.Progress.Id == id && !current.Progress.Retired
            ? current : null;

    private static bool IsPaused(SessionRuntime session) => session.Mode == ExecutionMode.Paused;

    private static GameUpdateStatus GetUpdateStatus(SessionRuntime? session)
        => session is null || session.Mode == ExecutionMode.Stopping ? GameUpdateStatus.Stopped
            : IsPaused(session) ? GameUpdateStatus.Deferred : GameUpdateStatus.Applied;

    // すべて EscapeAccess の同期境界内で扱います。State と通知の寿命をまとめる内部補助です。
    private sealed class SessionRuntime(long id)
    {
        internal readonly SessionProgressState Progress = new(id);
        internal readonly PlayerState Player = new();
        internal readonly InventoryState Inventory = new();
        internal readonly MechanismState Mechanisms = new();
        internal readonly EnemyState Enemy = new();
        internal readonly DisplayState Display = new();
        internal ExecutionMode Mode;
        internal long ControlRevision;
        internal TaskCompletionSource<GameCommand>? Input;
        internal CancellationToken InputToken;
        internal TaskCompletionSource<bool>? Resume;
        internal PresentationRuntime? Presentation;
        internal long NextPresentation;
    }

    private sealed class DisplayState
    {
        internal long Version;
        internal string Message = "Preparing room";

        internal void SetMessage(string message)
        {
            Message = message;
            Version++;
        }
    }

    private GameUpdateResult PublishEffect(SessionRuntime session, ActionEffect? effect)
    {
        if (effect is not { } value)
            return new(GameUpdateStatus.Rejected);
        session.Display.SetMessage(value.Message);
        return BeginPresentation(session, value.Kind, value.From, value.To, value.DurationSeconds);
    }

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

    private static bool IsPresentationComplete(SessionRuntime session)
        => session.Presentation?.Completion.Task.IsCompletedSuccessfully == true
            && session.Presentation.Completion.Task.Result == PresentationCompletion.Completed;

    private GameUpdateResult BeginPresentation(
        SessionRuntime session, PresentationKind kind, GridPosition from, GridPosition to, double duration)
    {
        var key = new PresentationKey(session.Progress.Id, ++session.NextPresentation, session.Display.Version);
        session.Presentation = new(new(key, kind, from, to, duration, 0, false));
        return new(GameUpdateStatus.Applied, key);
    }

    // EscapeAccessの同期境界内で、提示の進行と完了通知を保持します。
    private sealed class PresentationRuntime(PresentationSnapshot snapshot)
    {
        internal PresentationSnapshot Snapshot = snapshot;
        internal bool Started;
        internal readonly TaskCompletionSource<PresentationCompletion> Completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
