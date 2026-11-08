using System;

namespace InteractionFlow.Samples.Raylib.Entities;

// ゲームの状態と遷移規則だけを担当します。同期はアクセス管理側が行います。
internal sealed class CounterState
{
    private enum Phase
    {
        Ready,
        WaitingForInput,
        Processing,
        Exiting,
        Failed
    }

    private Phase phase;

    internal int Count { get; private set; }

    internal bool ExitRequested { get; private set; }

    internal bool CanBeginWait => phase == Phase.Ready;

    internal bool CanAcceptInput => phase == Phase.WaitingForInput && !ExitRequested;

    internal void BeginWait() => phase = Phase.WaitingForInput;

    internal void EndWait()
    {
        if (phase == Phase.WaitingForInput)
            phase = Phase.Ready;
    }

    internal void AcceptCommand() => phase = Phase.Processing;

    internal bool BeginIncrement()
    {
        if (ExitRequested)
            return false;

        RequireProcessing();
        return true;
    }

    internal bool CompleteIncrement()
    {
        if (ExitRequested)
            return false;

        RequireProcessing();
        Count++;
        phase = Phase.Ready;
        return true;
    }

    internal bool Cancel()
    {
        if (ExitRequested || phase == Phase.Failed)
            return false;

        phase = Phase.Ready;
        return true;
    }

    internal void Fail() => phase = Phase.Failed;

    internal bool RequestExit()
    {
        ExitRequested = true;

        if (phase == Phase.Failed)
            return false;

        phase = Phase.Exiting;
        return true;
    }

    private void RequireProcessing()
    {
        if (phase != Phase.Processing)
            throw new InvalidOperationException("Counter is not processing an accepted command.");
    }
}
