namespace InteractionFlow.Samples.EscapeGame.Entities;

public sealed class FrameState
{
    public long SessionId { get; set; }
    public GameCommand? Command { get; set; }
    public bool PauseRequested { get; set; }
    public bool SkipRequested { get; set; }
    public PresentationKey? Presentation { get; set; }
    public bool ShutdownRequested { get; set; }
}
