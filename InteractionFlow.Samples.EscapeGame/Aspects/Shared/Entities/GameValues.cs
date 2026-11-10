namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

public readonly record struct GridPosition(int X, int Y)
{
    public int DistanceTo(GridPosition other) => System.Math.Abs(X - other.X) + System.Math.Abs(Y - other.Y);
}

public enum GameCommand { Up, Right, Down, Left, Interact, Wait, Confirm, CancelSelection, Retry, Stop }

public enum GameOutcome { Running, Won, Lost }

public enum GameStage { Preparing, Selecting, PlayerPresenting, EnemyPresenting, Result, Stopping }

/// <summary>
/// 共有アクセスへの更新要求の結果です。規則上の拒否でも、通常更新ではFeedback等の状態を反映します。
/// </summary>
public enum GameUpdateStatus { Applied, Deferred, Stopped, Rejected }

public enum ActionKind { Move, Switch, Unlock, Wait }

public readonly record struct GameAction(ActionKind Kind, GridPosition Target);

/// <summary>
/// ゲーム規則による操作の成立可否と次状態です。拒否時にもFeedback等を反映した状態を保持します。
/// 共有アクセスが次状態を反映し、受付状況を含むGameUpdateResultへ変換します。
/// </summary>
public readonly record struct GameStateChange(bool Accepted, GameState State);

/// <summary>
/// 共有アクセスへの更新要求の結果と、その要求に対応する提示キーを返します。
/// </summary>
public readonly record struct GameUpdateResult(GameUpdateStatus Status, PresentationKey? Presentation = null);
