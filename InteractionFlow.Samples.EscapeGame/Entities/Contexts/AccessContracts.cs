using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

// ゲーム側が入力を待ち、論理状態を進め、その提示の完了を待つための契約です。
public interface IGameplayAccess
{
    long CreateSession();
    MutationResult BeginSession(long sessionId);
    void EndSession(long sessionId);
    GameProgress GetProgress(long sessionId);
    ValueTask<GameCommand> ReadAsync(long sessionId, CancellationToken token);
    MutationStatus ApplySelection(long sessionId, GameCommand command);
    MutationResult ApplyPlayer(long sessionId);
    MutationResult ApplyEnemy(long sessionId);
    MutationResult ShowResult(long sessionId);
    MutationStatus EnterSelecting(long sessionId);
    MutationStatus EnableResultInput(long sessionId);
    ValueTask<bool> WaitForActiveAsync(long sessionId, CancellationToken token);
    ValueTask<PresentationCompletion> WaitForPresentationAsync(PresentationKey key, CancellationToken token);
}

// 画面側が表示を読み、入力・制御要求と描画後の提示完了を通知するための契約です。
public interface IGameViewAccess
{
    GameSnapshot Snapshot();
    bool TrySubmit(long sessionId, GameCommand command);
    GameProgress GetProgress(long sessionId);
    bool TrySetPaused(long sessionId, bool paused);
    PresentationSnapshot? Advance(PresentationKey key, double elapsedSeconds, long controlRevision);
    bool TryComplete(PresentationKey key);
    bool TrySkip(PresentationKey key);
}

// 全体停止は、通常の両実行経路と共通Handlerから利用します。
public interface IApplicationLifetimeAccess
{
    bool ShutdownRequested { get; }
    void RequestShutdown();
}
