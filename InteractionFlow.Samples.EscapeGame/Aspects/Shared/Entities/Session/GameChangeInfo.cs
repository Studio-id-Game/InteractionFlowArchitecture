using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;

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
