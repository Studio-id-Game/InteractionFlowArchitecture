namespace InteractionFlow.Samples.Raylib.Entities;

public sealed class CounterState
{
    // Count はフローだけが読み書きし、UI は表示用スナップショットを読みます。
    public int Count { get; set; }

    // ウィンドウ終了要求だけはメインスレッドからも通知します。
    public volatile bool ExitRequested;
}
