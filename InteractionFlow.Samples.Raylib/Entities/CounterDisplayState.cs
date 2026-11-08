using System;

namespace InteractionFlow.Samples.Raylib.Entities;

// アクセス管理のロック内でのみ更新する表示状態です。
internal sealed class CounterDisplayState
{
    internal string Message { get; private set; } = "Ready";

    internal void BeginIncrement() => Message = "Working... (Cancel is available)";

    internal void CompleteIncrement() => Message = "Completed";

    internal void Cancel() => Message = "Canceled; ready again";

    internal void Fail(Exception exception) => Message = "Error: " + exception.Message;

    internal void RequestExit() => Message = "Goodbye";
}
