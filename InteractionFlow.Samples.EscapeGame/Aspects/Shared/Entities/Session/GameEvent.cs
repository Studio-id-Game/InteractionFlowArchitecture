using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;

// ゲーム内で起きた出来事。表示文言・秒数・経過時間は含めません。
public sealed record GameEvent(PresentationKey Key, GameEventKind Kind, GridPosition From, GridPosition To);

public enum GameEventKind { Initial, PlayerMove, Switch, Unlock, Wait, EnemyMove, Result }

public readonly record struct PresentationKey(long SessionId, long PresentationId, long Version);

public enum PresentationCompletion { Completed, Stopped }
