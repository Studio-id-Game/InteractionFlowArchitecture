namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

// 不変の共有ゲーム状態。取得したインスタンスはロック外でも安全に読めます。
public sealed record GameState(long SessionId)
{
    public static GameState Empty { get; } = new(0);
    public RoomDefinition Room => RoomDefinition.Default;
    public long Version { get; init; }
    public GridPosition Player { get; init; } = RoomDefinition.Default.PlayerStart;
    public int EnemyPatrolIndex { get; init; }
    public bool HasKey { get; init; }
    public bool KeyCollected { get; init; }
    public bool SwitchActivated { get; init; }
    public bool DoorUnlocked { get; init; }
    public int Turn { get; init; }
    public GameStage Stage { get; init; } = GameStage.Preparing;
    public GameOutcome Outcome { get; init; }
    public GameAction? Selection { get; init; }
    public bool Confirmed { get; init; }
    public bool RetryRequested { get; init; }
    public bool ResultInputEnabled { get; init; }
    // 現在の状態から復元できない直前の操作結果だけを保持します。
    public InputFeedback? Feedback { get; init; }
    public ExecutionMode Mode { get; init; }
    public long ControlRevision { get; init; }
    public bool WaitingForInput { get; init; }
    public GameEvent? Event { get; init; }
    public bool PresentationCompleted { get; init; }
    public bool SkipRequested { get; init; }
}

public enum InputFeedback
{
    SelectionCanceled, InvalidSelection, NoUsableMechanism, StageDoesNotAcceptSelection
}
