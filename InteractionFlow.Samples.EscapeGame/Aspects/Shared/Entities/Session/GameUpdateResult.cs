namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;

/// <summary>
/// 共有アクセスへの更新要求の結果と、その要求に対応する提示キーを返します。
/// </summary>
public readonly record struct GameUpdateResult(GameUpdateStatus Status, PresentationKey? Presentation = null);

/// <summary>
/// 共有アクセスへの更新要求の結果です。規則上の拒否でも、通常更新ではFeedback等の状態を反映します。
/// </summary>
public enum GameUpdateStatus { Applied, Deferred, Stopped, Rejected }
