using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

// ゲーム側が入力を待ち、論理状態を進め、その提示の完了を待つための契約です。
public interface IGameplayAccess
{
    long AllocateSession();
    MutationResult BeginSession(long sessionId);
    void EndSession(long sessionId);
    GameProgress GetSessionProgress(long sessionId);
    ValueTask<GameCommand> ReadCommandAsync(long sessionId, CancellationToken token);
    MutationStatus ApplyInputCommand(long sessionId, GameCommand command);
    MutationResult ApplyPlayerAction(long sessionId);
    MutationResult ApplyEnemyAction(long sessionId);
    MutationResult BeginResultPresentation(long sessionId);
    MutationStatus BeginActionSelection(long sessionId);
    MutationStatus EnableResultInput(long sessionId);
    ValueTask<bool> WaitForActiveAsync(long sessionId, CancellationToken token);
    ValueTask<PresentationCompletion> WaitForPresentationCompletionAsync(PresentationKey key, CancellationToken token);
}

// 画面側が表示を読み、入力・制御要求と描画後の提示完了を通知するための契約です。
public interface IGameViewAccess
{
    GameSnapshot CaptureSnapshot();
    bool TrySubmitCommand(long sessionId, GameCommand command);
    GameProgress GetSessionProgress(long sessionId);
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
