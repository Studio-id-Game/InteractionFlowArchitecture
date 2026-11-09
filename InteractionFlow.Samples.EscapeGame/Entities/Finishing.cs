using InteractionFlow.Core.Entities;

namespace InteractionFlow.Samples.EscapeGame.Entities;

public enum FinishingKind
{
    CloseWindow,
    EndSession
}

// 結果確定の専用スコープへだけ渡します。通常の Result 値と終了処理中の意味を分けます。
public sealed record Finishing(FinishingKind Kind, Result Previous);
