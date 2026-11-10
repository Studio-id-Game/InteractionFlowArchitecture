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
        {
            var session = FindActiveSession(sessionId);
            if (session is null || session.State.Mode != ExecutionMode.Active || command == GameCommand.Stop
                || session.Input is not { Task.IsCompleted: false } || session.InputToken.IsCancellationRequested)
                return false;
            var accepted = session.Input.TrySetResult(command);
            if (accepted) session.State = session.State with { WaitingForInput = false };
            return accepted;
        }
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
        {
            var session = FindActiveSession(key.SessionId);
            if (session is null || session.State.Mode != ExecutionMode.Active || session.State.Event?.Key != key
                || session.Presentation?.TrySetResult(PresentationCompletion.Completed) != true)
                return false;
            session.State = session.State with { PresentationCompleted = true };
            return true;
        }
    }
}
