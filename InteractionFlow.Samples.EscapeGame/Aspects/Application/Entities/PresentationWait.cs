using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities;

/// <summary>
/// 提示キーに対応する完了・停止通知を管理します。アクセスはEscapeAccess.gate内で行います。
/// </summary>
internal sealed class PresentationWait
{
    private PresentationKey? key;
    private TaskCompletionSource<PresentationCompletion>? source;

    /// <summary>
    /// 提示キーが変わった場合に旧待機へ停止を通知し、新しい提示の待機を作成します。
    /// キーがnullなら待機を解除し、同じキーなら完了状態を含めて現在の待機を保ちます。
    /// </summary>
    internal void SetEvent(PresentationKey? nextKey)
    {
        if (key == nextKey)
            return;

        Stop();
        key = nextKey;
        source = nextKey is null ? null : new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// キーが現在の提示と一致する場合に通知Taskを返します。完了済みのTaskもそのまま返します。
    /// </summary>
    internal Task<PresentationCompletion>? GetTask(PresentationKey requestedKey)
        => key == requestedKey ? source?.Task : null;

    /// <summary>
    /// キーが一致する未完了の提示に完了を通知し、受理できた場合だけtrueを返します。
    /// </summary>
    internal bool TryComplete(PresentationKey requestedKey)
        => key == requestedKey && source?.TrySetResult(PresentationCompletion.Completed) == true;

    /// <summary>
    /// 残っている提示待機へ停止を通知します。現在のキーと通知Taskは保持します。
    /// </summary>
    internal void Stop() => source?.TrySetResult(PresentationCompletion.Stopped);
}
