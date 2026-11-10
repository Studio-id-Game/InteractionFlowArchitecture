using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using System.Threading.Tasks;
using System.Threading;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;

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
