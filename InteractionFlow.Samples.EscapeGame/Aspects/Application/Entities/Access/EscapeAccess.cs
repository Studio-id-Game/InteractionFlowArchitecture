using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;
using System.Collections.Generic;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities.Access;

internal sealed partial class EscapeAccess : IGameplayAccess, IViewAccess, ILifetimeAccess
{
    private readonly object gate = new();
    private readonly Dictionary<long, SessionRuntime> sessions = new();
    private SessionRuntime? active;
    private GameControlInput? controlInput;
    private long nextSession;
    private bool shutdown;

    private SessionRuntime? FindActiveSession(long id)
        => !shutdown
            && active is { Mode: not ExecutionMode.Stopping } current
            && current.State.SessionId == id
                ? current
                : null;
}
