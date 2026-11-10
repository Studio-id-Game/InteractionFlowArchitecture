using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Access;

internal sealed partial class EscapeAccess
{
    public GameState ReadGameState()
    {
        lock (gate)
            return active?.State ?? (shutdown ? GameState.Empty with { Mode = ExecutionMode.Stopping } : GameState.Empty);
    }

    public bool TrySubmitCommand(long sessionId, GameCommand command)
    {
        lock (gate)
            return FindActiveSession(sessionId)?.TrySubmitCommand(command) ?? false;
    }

    public bool TrySubmitControl(GameControlInput input)
    {
        lock (gate)
        {
            if (FindActiveSession(input.SessionId) is null) return false;
            controlInput = input;
            return true;
        }
    }

    public bool TryCompletePresentation(PresentationKey key)
    {
        lock (gate)
            return FindActiveSession(key.SessionId)?.TryCompletePresentation(key) ?? false;
    }
}
