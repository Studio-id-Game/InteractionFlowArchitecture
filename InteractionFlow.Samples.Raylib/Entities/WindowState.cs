using System;

namespace InteractionFlow.Samples.Raylib.Entities;

// フレーム用 Context で共有します。ネイティブ API の呼び出しやリソース所有は行いません。
public sealed class WindowState
{
    private int ownerThread;

    public WindowState()
    {
    }

    public bool IsOpen { get; private set; }

    public void MarkOpened()
    {
        if (IsOpen)
            throw new InvalidOperationException("Window is already open.");

        ownerThread = Environment.CurrentManagedThreadId;
        IsOpen = true;
    }

    public void MarkClosed()
    {
        VerifyAccess();
        IsOpen = false;
    }

    public void VerifyAccess()
    {
        if (!IsOpen || ownerThread != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("Window access requires its opening thread.");
    }
}
