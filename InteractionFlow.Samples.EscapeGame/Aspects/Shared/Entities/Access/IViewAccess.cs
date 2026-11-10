namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;

// Viewはゲーム状態を読み、操作と提示完了を入力として渡します。
public interface IViewAccess
{
    GameState ReadGameState();
    bool TrySubmitCommand(long sessionId, GameCommand command);
    bool TrySubmitControl(GameControlInput input);
    bool TryCompletePresentation(PresentationKey key);
}
