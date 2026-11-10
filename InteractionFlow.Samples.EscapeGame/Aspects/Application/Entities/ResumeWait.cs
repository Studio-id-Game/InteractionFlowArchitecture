using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities;

/// <summary>
/// ポーズからの再開・停止通知を管理します。アクセスはEscapeAccess.gate内で行います。
/// </summary>
internal sealed class ResumeWait
{
    private TaskCompletionSource<bool>? source;

    /// <summary>
    /// ポーズ開始時に作成した通知Taskを取得します。ポーズ中にだけ参照します。
    /// </summary>
    internal Task<bool> Task => source!.Task;

    /// <summary>
    /// モード変更時に呼び出し、ポーズでは待機を作成し、解除時はActiveかどうかを通知して解除します。
    /// </summary>
    internal void ChangeMode(ExecutionMode mode)
    {
        if (mode == ExecutionMode.Paused)
            source = new(TaskCreationOptions.RunContinuationsAsynchronously);
        else
        {
            source?.TrySetResult(mode == ExecutionMode.Active);
            source = null;
        }
    }

    /// <summary>
    /// 残っている再開待機へfalseを通知し、進行を打ち切ります。
    /// </summary>
    internal void Stop() => source?.TrySetResult(false);
}
