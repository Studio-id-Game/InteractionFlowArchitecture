using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

// ゲーム側が入力を待ち、論理状態を進め、その提示の完了を待つための契約です。
public interface IGameplayAccess
{
    long AllocateSession();
    GameUpdateResult BeginSession(long sessionId);
    void EndSession(long sessionId);
    SessionProgress GetSessionProgress(long sessionId);
    ValueTask<GameCommand> ReadCommandAsync(long sessionId, CancellationToken token);
    GameUpdateStatus ApplyInputCommand(long sessionId, GameCommand command);
    GameUpdateResult ApplyPlayerAction(long sessionId);
    GameUpdateResult ApplyEnemyAction(long sessionId);
    GameUpdateResult BeginResultPresentation(long sessionId);
    GameUpdateStatus BeginActionSelection(long sessionId);
    GameUpdateStatus EnableResultInput(long sessionId);
    ValueTask<bool> WaitForActiveAsync(long sessionId, CancellationToken token);
    ValueTask<PresentationCompletion> WaitForPresentationCompletionAsync(PresentationKey key, CancellationToken token);
}

// 画面側が表示を読み、入力・制御要求と描画後の提示完了を通知するための契約です。
public interface IGameViewAccess
{
    GameViewSnapshot CaptureSnapshot();
    bool TrySubmitCommand(long sessionId, GameCommand command);
    SessionProgress GetSessionProgress(long sessionId);
    bool TrySetPaused(long sessionId, bool paused);
    PresentationSnapshot? AdvancePresentation(PresentationKey key, double elapsedSeconds, long controlRevision);
    bool TryCompletePresentation(PresentationKey key);
    bool TryRequestPresentationSkip(PresentationKey key);
}

// 全体停止は、通常の両実行経路と共通Handlerから利用します。
public interface IApplicationLifetimeAccess
{
    bool ShutdownRequested { get; }
    void RequestShutdown();
}
