namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;

// Viewはゲーム状態を読み、操作と提示完了を入力として渡します。
public interface IViewAccess
{
    /// <summary>
    /// 直近に公開された不変ゲーム状態と、描画・入力に必要なセッション情報を同時に取得します。
    /// セッション停止後もその最終状態を返し、まだ一度も公開されていなければ空の状態を返します。
    /// </summary>
    GameInfo<GameViewInfo> ReadGameState();

    /// <summary>
    /// 指定セッションがActiveで、未完了かつ取消前の入力待機がある場合に通常入力を一件配送します。
    /// 配送できなければfalseを返し、入力は蓄積しません。Stopは配送対象外です。
    /// 行動がゲーム規則上有効かどうかはGameplayが判定します。
    /// </summary>
    bool TrySubmitCommand(long sessionId, GameCommand command);

    /// <summary>
    /// 停止していない公開中セッションへの制御要求を、最新一件として保持します。
    /// 保持中の要求は上書きし、対象が無効ならfalseを返します。状態への適用はGameplayによる読み取り後に行います。
    /// </summary>
    bool TrySubmitControl(GameControlInput input);

    /// <summary>
    /// Viewが描画を提出した後、指定キーの提示完了を通知してゲーム進行側の待機を解除します。
    /// Activeな現行セッションの一致する未完了提示だけを受理し、その他はfalseを返します。
    /// </summary>
    bool TryCompletePresentation(PresentationKey key);
}
