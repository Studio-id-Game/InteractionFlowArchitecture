using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;

// 画面側が表示を読み、入力・制御要求と描画後の提示完了を通知するための契約です。
public interface IViewAccess
{
    ViewSnapshot CaptureSnapshot();
    bool TrySubmitCommand(long sessionId, GameCommand command);
    SessionProgress GetSessionProgress(long sessionId);
    bool TrySetPaused(long sessionId, bool paused);
    PresentationSnapshot? AdvancePresentation(PresentationKey key, double elapsedSeconds, long controlRevision);
    bool TryCompletePresentation(PresentationKey key);
    bool TryRequestPresentationSkip(PresentationKey key);
}
