namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

public enum GameEventKind { Initial, PlayerMove, Switch, Unlock, Wait, EnemyMove, Result }
public enum PresentationCompletion { Completed, Stopped }
public readonly record struct PresentationKey(long SessionId, long PresentationId, long Version);

// ゲーム内で起きた出来事。表示文言・秒数・経過時間は含めません。
public sealed record GameEvent(PresentationKey Key, GameEventKind Kind, GridPosition From, GridPosition To);
