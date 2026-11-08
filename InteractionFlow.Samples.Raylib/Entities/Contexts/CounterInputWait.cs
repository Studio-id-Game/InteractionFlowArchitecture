using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.Entities.Contexts;

// 通知の仕組みだけを保持します。呼び出し側が共通のロックを保持します。
internal sealed class CounterInputWait
{
    private TaskCompletionSource<CounterCommand>? source;
    private CancellationToken token;

    internal bool HasPending => source is not null;

    internal bool CanAccept => source is { Task.IsCompleted: false } && !token.IsCancellationRequested;

    internal TaskCompletionSource<CounterCommand> Begin(CancellationToken token)
    {
        if (source is not null)
            throw new InvalidOperationException("An input wait is already active.");

        this.token = token;
        source = new(TaskCreationOptions.RunContinuationsAsynchronously);
        return source;
    }

    internal void Submit(CounterCommand command) => source!.SetResult(command);

    internal bool Release(TaskCompletionSource<CounterCommand> completed)
    {
        if (!ReferenceEquals(source, completed))
            return false;

        source = null;
        token = default;
        return true;
    }
}
