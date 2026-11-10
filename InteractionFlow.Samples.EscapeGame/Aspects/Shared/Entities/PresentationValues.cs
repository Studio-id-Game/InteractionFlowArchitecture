namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

public enum PresentationKind { Initial, PlayerMove, Switch, Unlock, Wait, EnemyMove, Result }

public enum PresentationCompletion { Completed, Stopped }

public readonly record struct PresentationKey(long SessionId, long PresentationId, long Version);

public sealed record PresentationSnapshot(PresentationKey Key, PresentationKind Kind,
    GridPosition From, GridPosition To, double DurationSeconds, double ElapsedSeconds, bool SkipRequested);
