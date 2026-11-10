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
/// ゲーム状態と、用途ごとに必要な付帯情報をまとめます。
/// </summary>
public sealed record GameInfo<T>(GameState State, T Info);

/// <summary>
/// ゲーム規則による操作の成立可否と、セッションへの反映指示です。未指定の情報は保持します。
/// </summary>
public sealed record GameChangeInfo(bool Accepted)
{
    /// <summary> trueならFeedbackを設定します。値がnullなら解除します。 </summary>
    public bool ReplaceFeedback { get; init; }
    public InputFeedback? Feedback { get; init; }

    /// <summary> trueなら提示を置き換えます。種類がnullなら解除し、キーはSessionRuntimeが発行します。 </summary>
    public bool ReplacePresentation { get; init; }
    public GameEventKind? Presentation { get; init; }
    public GridPosition From { get; init; }
    public GridPosition To { get; init; }
    public ExecutionMode? Mode { get; init; }
    public bool RequestSkip { get; init; }
}

/// <summary>
/// Viewがゲーム状態と同時に取得する不変のセッション情報です。表示文字列や演出時間は含みません。
/// </summary>
public sealed record GameViewInfo(
    InputFeedback? Feedback = null,
    ExecutionMode Mode = ExecutionMode.Active,
    bool WaitingForInput = false,
    GameEvent? Event = null,
    bool SkipRequested = false,
    long ControlRevision = 0);

/// <summary>
/// 共有アクセスへの更新要求の結果と、その要求に対応する提示キーを返します。
/// </summary>
public readonly record struct GameUpdateResult(GameUpdateStatus Status, PresentationKey? Presentation = null);
