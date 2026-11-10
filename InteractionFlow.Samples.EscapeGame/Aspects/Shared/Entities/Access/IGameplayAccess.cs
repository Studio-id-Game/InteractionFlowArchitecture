using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;

public interface IGameplayAccess
{
    long AllocateSession();
    GameUpdateResult BeginSession(long sessionId, Func<GameState, GameUpdateResult> initialize);
    void EndSession(long sessionId);
    GameState ReadGameState(long sessionId);
    SessionProgress GetSessionProgress(long sessionId);
    // 更新関数は同期的に実行し、返した不変状態を一括で反映します。
    GameUpdateResult UpdateGameState(long sessionId, Func<GameState, GameUpdateResult> update,
        bool allowWhilePaused = false);
    ValueTask<GameCommand> ReadCommandAsync(long sessionId, CancellationToken token);
    bool TryReadControlInput(out GameControlInput input);
    ValueTask<bool> WaitForActiveAsync(long sessionId, CancellationToken token);
    ValueTask<PresentationCompletion> WaitForPresentationCompletionAsync(PresentationKey key, CancellationToken token);
}
