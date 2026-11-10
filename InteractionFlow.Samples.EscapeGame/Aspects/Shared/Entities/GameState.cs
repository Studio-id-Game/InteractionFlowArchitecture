namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

// 不変の共有ゲーム状態。取得したインスタンスはロック外でも安全に読めます。
public sealed record GameState(long SessionId)
{
    public static GameState Empty { get; } = new(0);
    public RoomDefinition Room => RoomDefinition.Default;
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
}

public enum InputFeedback
{
    SelectionCanceled, InvalidSelection, NoUsableMechanism, StageDoesNotAcceptSelection
}
