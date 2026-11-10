using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

public sealed record ViewSnapshot(long SessionId, long Version, RoomDefinition Room,
    GridPosition Player, GridPosition Enemy, int EnemyPatrolIndex, bool HasKey, bool KeyCollected,
    bool SwitchActivated, bool DoorUnlocked, int Turn, GameStage Stage, GameOutcome Outcome,
    ExecutionMode Mode, long ControlRevision, GameAction? Selection, string Message,
    bool AcceptingInput, bool CanConfirm, bool RetryRequested, bool ShutdownRequested,
    PresentationSnapshot? Presentation);
