using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities;

/// <summary>
/// SessionRuntimeが所有する、受け渡し・提示・実行制御の不変状態です。操作はEscapeAccess.gateで保護します。
/// </summary>
internal sealed record SessionState
{
    internal long Version { get; init; }
    internal InputFeedback? Feedback { get; init; }
    internal ExecutionMode Mode { get; init; }
    internal long ControlRevision { get; init; }
    internal bool WaitingForInput { get; init; }
    internal GameEvent? Event { get; init; }
    internal bool PresentationCompleted { get; init; }
    internal bool SkipRequested { get; init; }
}
