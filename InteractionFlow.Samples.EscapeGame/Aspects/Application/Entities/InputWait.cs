using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Game;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities;

/// <summary>
/// 入力受信先と配送時の取消判定用Tokenを管理します。アクセスはEscapeAccess.gate内で行います。
/// </summary>
internal sealed class InputWait
{
    private TaskCompletionSource<GameCommand>? source;
    private CancellationToken token;

    /// <summary>
    /// 一件の入力待機を登録します。完了済みでもEndによる解除までは次の登録を拒否します。
    /// </summary>
    internal Task<GameCommand> Begin(CancellationToken cancellationToken)
    {
        if (source is not null)
            throw new InvalidOperationException("Only one input wait is supported.");

        source = new(TaskCreationOptions.RunContinuationsAsynchronously);
        token = cancellationToken;

        return source.Task;
    }

    /// <summary>
    /// 登録した待機自身のfinallyから呼び出し、受信先とTokenを解除して次の登録を可能にします。
    /// </summary>
    internal void End()
    {
        source = null;
        token = default;
    }

    /// <summary>
    /// 未完了かつ取消前の受信先へ通常入力を配送します。Stop要求は拒否します。
    /// </summary>
    internal bool TrySubmit(GameCommand command)
        => command != GameCommand.Stop
            && source is { Task.IsCompleted: false }
            && !token.IsCancellationRequested
            && source.TrySetResult(command);

    /// <summary>
    /// 残っている入力待機へStopを通知します。受信先の解除はEndに任せます。
    /// </summary>
    internal void Stop() => source?.TrySetResult(GameCommand.Stop);
}
