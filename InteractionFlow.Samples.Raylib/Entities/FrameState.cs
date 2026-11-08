namespace InteractionFlow.Samples.Raylib.Entities;

public sealed class FrameState
{
    public CounterCommand? Command { get; set; }

    public bool CloseRequested { get; set; }

    public bool CancelRequested { get; set; }
}
