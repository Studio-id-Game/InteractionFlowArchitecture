namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Session;

public readonly record struct GameControlInput(long SessionId, GameControlCommand Command,
    PresentationKey? Presentation = null);

public enum GameControlCommand { TogglePause, Skip }
