namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

public enum GameControlCommand { TogglePause, Skip }
public readonly record struct GameControlInput(long SessionId, GameControlCommand Command,
    PresentationKey? Presentation = null);
