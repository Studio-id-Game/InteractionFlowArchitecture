using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Entities.Contexts;

// すべて EscapeAccess の同期境界内で扱います。State と通知の寿命をまとめる内部補助です。
internal sealed class SessionRuntime(long id)
{
    internal readonly SessionProgressState Progress = new(id);
    internal readonly PlayerState Player = new();
    internal readonly InventoryState Inventory = new();
    internal readonly MechanismState Mechanisms = new();
    internal readonly EnemyState Enemy = new();
    internal readonly DisplayState Display = new();
    internal ExecutionMode Mode;
    internal long ControlRevision;
    internal TaskCompletionSource<GameCommand>? Input;
    internal CancellationToken InputToken;
    internal TaskCompletionSource<bool>? Resume;
    internal PresentationRuntime? Presentation;
    internal long NextPresentation;
}

internal sealed class PresentationRuntime(PresentationSnapshot snapshot)
{
    internal PresentationSnapshot Snapshot = snapshot;
    internal bool Started;
    internal readonly TaskCompletionSource<PresentationCompletion> Completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
