namespace InteractionFlow.Samples.Raylib.Entities;

// 同じインスタンスを両フローの Context で共有し、表示内容の取得・更新を同期します。
public sealed class CounterDisplayState
{
    private readonly object gate = new();
    private int count;
    private string message = "Ready";
    private bool acceptingInput;

    public void Snapshot(out int count, out string message, out bool acceptingInput)
    {
        lock (gate)
        {
            count = this.count;
            message = this.message;
            acceptingInput = this.acceptingInput;
        }
    }

    public void Show(int count, string message)
    {
        lock (gate)
        {
            this.count = count;
            this.message = message;
            acceptingInput = false;
        }
    }

    public void SetAcceptingInput(bool accepting)
    {
        lock (gate)
            acceptingInput = accepting;
    }
}
