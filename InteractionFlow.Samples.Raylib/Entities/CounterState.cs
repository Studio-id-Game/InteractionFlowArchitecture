using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.Entities;

public sealed class CounterState
{
    private enum Phase
    {
        Ready,
        WaitingForInput,
        Processing,
        Exiting,
        Failed
    }

    // 状態と入力通知を同じロックで管理し、受付表示と受理条件を一致させます。
    private readonly object gate = new();

    private int count;
    private string message = "Ready";
    private Phase phase;
    private bool exitRequested;

    // 通知は待機中だけ保持し、継続処理をロック内で実行しません。
    private TaskCompletionSource<CounterCommand>? pending;
    private CancellationToken pendingToken;

    // 状態の取得。描画用の値は Snapshot でまとめて取得します。
    public int Count
    {
        get
        {
            lock (gate)
                return count;
        }
    }

    public bool ExitRequested
    {
        get
        {
            lock (gate)
                return exitRequested;
        }
    }

    public void Snapshot(out int count, out string message, out bool acceptingInput)
    {
        lock (gate)
        {
            count = this.count;
            message = this.message;
            acceptingInput = CanAcceptInput();
        }
    }

    // 入力待機と受理。終了要求は受付を閉じ、既存待機の解除は所有者のキャンセルに任せます。
    public async ValueTask<CounterCommand> ReadCommandAsync(CancellationToken token)
    {
        TaskCompletionSource<CounterCommand> source;

        lock (gate)
        {
            token.ThrowIfCancellationRequested();

            if (exitRequested)
                return CounterCommand.Exit;

            if (pending is not null || phase != Phase.Ready)
                throw new InvalidOperationException("Counter is not ready for a new input wait.");

            source = new(TaskCreationOptions.RunContinuationsAsynchronously);
            pending = source;
            pendingToken = token;
            phase = Phase.WaitingForInput;
        }

        try
        {
            return await source.Task.WaitAsync(token).ConfigureAwait(false);
        }
        finally
        {
            lock (gate)
            {
                if (ReferenceEquals(pending, source))
                {
                    pending = null;
                    pendingToken = default;

                    if (phase == Phase.WaitingForInput)
                        phase = Phase.Ready;
                }
            }
        }
    }

    public bool TrySubmit(CounterCommand command)
    {
        lock (gate)
        {
            if (!CanAcceptInput())
                return false;

            phase = Phase.Processing;
            pending!.SetResult(command);
            return true;
        }
    }

    // 以下の判定は gate のロック内で呼び出します。
    private bool CanAcceptInput()
        => phase == Phase.WaitingForInput && !exitRequested
            && pending is { Task.IsCompleted: false } && !pendingToken.IsCancellationRequested;

    // 処理の進行。受理済みの操作だけが進み、終了要求後は更新しません。
    public bool BeginIncrement()
    {
        lock (gate)
        {
            if (exitRequested)
                return false;

            RequireProcessing();
            message = "Working... (Cancel is available)";
            return true;
        }
    }

    public bool CompleteIncrement()
    {
        lock (gate)
        {
            if (exitRequested)
                return false;

            RequireProcessing();
            count++;
            message = "Completed";
            phase = Phase.Ready;
            return true;
        }
    }

    // phase の検査は gate のロック内で行います。
    private void RequireProcessing()
    {
        if (phase != Phase.Processing)
            throw new InvalidOperationException("Counter is not processing an accepted command.");
    }

    // 中断・失敗・終了。キャンセル回復で終了意図や失敗を上書きしません。
    public void Cancel()
    {
        lock (gate)
        {
            // Interaction の本体（入力待機を含む）が終わってから回復します。
            if (pending is not null)
                throw new InvalidOperationException("Input wait must finish before cancellation recovery.");

            if (exitRequested || phase == Phase.Failed)
                return;

            phase = Phase.Ready;
            message = "Canceled; ready again";
        }
    }

    public void Fail(Exception exception)
    {
        lock (gate)
        {
            phase = Phase.Failed;
            message = "Error: " + exception.Message;
        }
    }

    public void RequestExit()
    {
        lock (gate)
        {
            exitRequested = true;

            if (phase != Phase.Failed)
            {
                phase = Phase.Exiting;
                message = "Goodbye";
            }
        }
    }
}
