namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;

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
