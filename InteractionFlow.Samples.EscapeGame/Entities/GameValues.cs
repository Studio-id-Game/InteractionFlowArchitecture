namespace InteractionFlow.Samples.EscapeGame.Entities;

public readonly record struct GridPosition(int X, int Y)
{
    public int DistanceTo(GridPosition other) => System.Math.Abs(X - other.X) + System.Math.Abs(Y - other.Y);
}

public enum GameCommand { Up, Right, Down, Left, Interact, Wait, Confirm, CancelSelection, Retry, Stop }
public enum GameOutcome { Running, Won, Lost }
public enum GameStage { Preparing, Selecting, PlayerPresenting, EnemyPresenting, Result, Stopping }
public enum ExecutionMode { Active, Paused, Stopping }
public enum GameUpdateStatus { Applied, Deferred, Stopped, Rejected }
public enum ActionKind { Move, Switch, Unlock, Wait }
public enum PresentationKind { Initial, PlayerMove, Switch, Unlock, Wait, EnemyMove, Result }
public enum PresentationCompletion { Completed, Stopped }

public readonly record struct GameAction(ActionKind Kind, GridPosition Target);
public readonly record struct PresentationKey(long SessionId, long PresentationId, long Version);
public readonly record struct GameUpdateResult(GameUpdateStatus Status, PresentationKey? Presentation = null);
public readonly record struct SessionProgress(GameStage Stage, GameOutcome Outcome, bool HasConfirmedAction,
    bool RetryRequested, bool Stopping, bool Paused);

public sealed record PresentationSnapshot(PresentationKey Key, PresentationKind Kind,
    GridPosition From, GridPosition To, double DurationSeconds, double ElapsedSeconds, bool SkipRequested);

public sealed record GameViewSnapshot(long SessionId, long Version, RoomDefinition Room,
    GridPosition Player, GridPosition Enemy, int EnemyPatrolIndex, bool HasKey, bool KeyCollected,
    bool SwitchActivated, bool DoorUnlocked, int Turn, GameStage Stage, GameOutcome Outcome,
    ExecutionMode Mode, long ControlRevision, GameAction? Selection, string Message,
    bool AcceptingInput, bool CanConfirm, bool RetryRequested, bool ShutdownRequested,
    PresentationSnapshot? Presentation);
