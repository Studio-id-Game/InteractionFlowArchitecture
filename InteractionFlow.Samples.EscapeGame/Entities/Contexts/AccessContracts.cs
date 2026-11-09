using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

public interface IGameInputAccess
{
    ValueTask<GameCommand> ReadAsync(long sessionId, CancellationToken token);
    bool TrySubmit(long sessionId, GameCommand command);
}
// セッションの公開・寿命・進行参照。ゲーム内の行動更新とは区別します。
public interface IGameSessionAccess
{
    long ActiveSessionId { get; }
    bool ShutdownRequested { get; }
    GameProgress GetProgress(long sessionId);
    MutationResult BeginSession(long sessionId);
    void EndSession(long sessionId);
    void RequestShutdown();
}
// 同じ同期管理の下で行う、選択・行動・提示段階の更新です。
public interface IGameWorldAccess
{
    MutationStatus ApplySelection(long sessionId, GameCommand command);
    MutationResult ApplyPlayer(long sessionId);
    MutationResult ApplyEnemy(long sessionId);
    MutationResult ShowResult(long sessionId);
    MutationStatus EnterSelecting(long sessionId);
    MutationStatus EnableResultInput(long sessionId);
}
public interface IGameDisplayAccess { GameSnapshot Snapshot(); }
public interface IGameControlAccess
{
    bool TrySetPaused(long sessionId, bool paused);
    ValueTask<bool> WaitForActiveAsync(long sessionId, CancellationToken token);
}
public interface IGamePresentationAccess
{
    ValueTask<PresentationCompletion> WaitAsync(PresentationKey key, CancellationToken token);
    PresentationSnapshot? Advance(PresentationKey key, double elapsedSeconds, long controlRevision);
    bool TryComplete(PresentationKey key);
    bool TrySkip(PresentationKey key);
}
