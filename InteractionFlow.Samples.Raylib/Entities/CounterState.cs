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

    private readonly object gate = new();
    private int count;
    private string message = "Ready";
    private Phase phase;
    private bool exitRequested;
    private TaskCompletionSource<CounterCommand>? pending;
    private CancellationToken pendingToken;

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

    private bool CanAcceptInput()
        => phase == Phase.WaitingForInput && !exitRequested
            && pending is { Task.IsCompleted: false } && !pendingToken.IsCancellationRequested;

    private void RequireProcessing()
    {
        if (phase != Phase.Processing)
            throw new InvalidOperationException("Counter is not processing an accepted command.");
    }
}
