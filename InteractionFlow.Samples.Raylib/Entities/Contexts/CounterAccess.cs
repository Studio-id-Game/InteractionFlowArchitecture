using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.Entities.Contexts;

// 分割した状態を一つの同期境界で扱います。状態そのものは外へ返しません。
internal sealed class CounterAccess : ICounterInputAccess, ICounterStateAccess, ICounterDisplayAccess
{
    private readonly object gate = new();
    private readonly CounterState state = new();
    private readonly CounterDisplayState display = new();
    private readonly CounterInputWait input = new();

    int ICounterStateAccess.Count
    {
        get
        {
            lock (gate)
                return state.Count;
        }
    }

    bool ICounterStateAccess.ExitRequested
    {
        get
        {
            lock (gate)
                return state.ExitRequested;
        }
    }

    void ICounterDisplayAccess.Snapshot(out int count, out string message, out bool acceptingInput)
    {
        lock (gate)
        {
            count = state.Count;
            message = display.Message;
            acceptingInput = CanAcceptInput();
        }
    }

    async ValueTask<CounterCommand> ICounterInputAccess.ReadCommandAsync(CancellationToken token)
    {
        TaskCompletionSource<CounterCommand> source;

        lock (gate)
        {
            token.ThrowIfCancellationRequested();

            if (state.ExitRequested)
                return CounterCommand.Exit;

            if (input.HasPending || !state.CanBeginWait)
                throw new InvalidOperationException("Counter is not ready for a new input wait.");

            source = input.Begin(token);
            state.BeginWait();
        }

        try
        {
            // 待機の間はロックを保持せず、完了後に改めて同期境界へ入ります。
            return await source.Task.WaitAsync(token).ConfigureAwait(false);
        }
        finally
        {
            lock (gate)
            {
                if (input.Release(source))
                    state.EndWait();
            }
        }
    }

    bool ICounterInputAccess.TrySubmit(CounterCommand command)
    {
        lock (gate)
        {
            if (!CanAcceptInput())
                return false;

            state.AcceptCommand();
            input.Submit(command);
            return true;
        }
    }

    // Snapshot と受理が同じ条件を参照します。gate のロック内で呼び出します。
    private bool CanAcceptInput() => state.CanAcceptInput && input.CanAccept;

    bool ICounterStateAccess.BeginIncrement()
    {
        lock (gate)
        {
            if (!state.BeginIncrement())
                return false;

            display.BeginIncrement();
            return true;
        }
    }

    bool ICounterStateAccess.CompleteIncrement()
    {
        lock (gate)
        {
            if (!state.CompleteIncrement())
                return false;

            display.CompleteIncrement();
            return true;
        }
    }

    void ICounterStateAccess.Cancel()
    {
        lock (gate)
        {
            if (input.HasPending)
                throw new InvalidOperationException("Input wait must finish before cancellation recovery.");

            if (state.Cancel())
                display.Cancel();
        }
    }

    void ICounterStateAccess.Fail(Exception exception)
    {
        lock (gate)
        {
            state.Fail();
            display.Fail(exception);
        }
    }

    void ICounterStateAccess.RequestExit()
    {
        lock (gate)
        {
            if (state.RequestExit())
                display.RequestExit();
        }
    }
}
