using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        => !shutdown && active is { Retired: false } current && current.State.SessionId == id ? current : null;

    private sealed class SessionRuntime(long id)
    {
        internal GameState State = new(id);
        internal bool Published;
        internal bool Retired;
        internal TaskCompletionSource<GameCommand>? Input;
        internal CancellationToken InputToken;
        internal TaskCompletionSource<bool>? Resume;
        internal TaskCompletionSource<PresentationCompletion>? Presentation;

        // 同期は呼び出し元のEscapeAccess.gateで行います。
        internal void StopSession()
        {
            Retired = true;
            State = State with { Mode = ExecutionMode.Stopping, Stage = GameStage.Stopping,
                WaitingForInput = false, ControlRevision = State.ControlRevision + 1 };
            Input?.TrySetResult(GameCommand.Stop);
            Resume?.TrySetResult(false);
            Presentation?.TrySetResult(PresentationCompletion.Stopped);
        }

        internal GameUpdateResult Commit(GameUpdateResult change)
        {
            var next = change.State ?? throw new InvalidOperationException("An update must return game state.");
            if (next.SessionId != State.SessionId || next.Event is { } e && e.Key.SessionId != next.SessionId)
                throw new InvalidOperationException("An update cannot change its session identity.");
            var previous = State;
            if (next.Version != previous.Version + 1)
                next = next with { Version = previous.Version + 1 };
            State = next;
            if (previous.Event?.Key != next.Event?.Key)
            {
                Presentation?.TrySetResult(PresentationCompletion.Stopped);
                Presentation = next.Event is null ? null : new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            if (previous.Mode != next.Mode)
            {
                if (next.Mode == ExecutionMode.Paused)
                    Resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
                else
                {
                    Resume?.TrySetResult(next.Mode == ExecutionMode.Active);
                    Resume = null;
                }
            }
            if (next.Mode == ExecutionMode.Stopping)
                StopSession();
            return change with { State = State };
        }
    }
}
