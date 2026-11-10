using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Access;

internal sealed partial class EscapeAccess
{
    private static readonly GameInfo<GameViewInfo> emptyView = new(GameState.Empty, new());
    private static readonly GameInfo<GameViewInfo> stoppedView =
        new(GameState.Empty, new(Mode: ExecutionMode.Stopping));

    public GameInfo<GameViewInfo> ReadGameState()
    {
        lock (gate)
            return active?.View ?? (shutdown ? stoppedView : emptyView);
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
