using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Access;

internal sealed partial class EscapeAccess
{
    public bool ShutdownRequested
    {
        get
        {
            lock (gate)
                return shutdown;
        }
    }

    public void RequestShutdown()
    {
        lock (gate)
        {
            if (shutdown) return;
            shutdown = true;
            foreach (var session in sessions.Values) StopSession(session);
            sessions.Clear();
        }
    }
}
