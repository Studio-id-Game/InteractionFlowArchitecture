namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

public readonly record struct GridPosition(int X, int Y)
{
    public int DistanceTo(GridPosition other) => System.Math.Abs(X - other.X) + System.Math.Abs(Y - other.Y);
}

public enum GameCommand { Up, Right, Down, Left, Interact, Wait, Confirm, CancelSelection, Retry, Stop }

public enum GameOutcome { Running, Won, Lost }

public enum GameStage { Preparing, Selecting, PlayerPresenting, EnemyPresenting, Result, Stopping }

public enum GameUpdateStatus { Applied, Deferred, Stopped, Rejected }

public enum ActionKind { Move, Switch, Unlock, Wait }

public readonly record struct GameAction(ActionKind Kind, GridPosition Target);

public readonly record struct GameUpdateResult(GameUpdateStatus Status, GameState? State = null)
{
    public PresentationKey? Presentation => State?.Event?.Key;
}
