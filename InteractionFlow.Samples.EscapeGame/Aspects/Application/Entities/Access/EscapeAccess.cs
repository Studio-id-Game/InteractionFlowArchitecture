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
        => !shutdown
            && active is { State.Mode: not ExecutionMode.Stopping } current
            && current.State.SessionId == id
                ? current
                : null;

    private sealed class SessionRuntime(long id)
    {
        private TaskCompletionSource<GameCommand>? input;
        private CancellationToken inputToken;
        private TaskCompletionSource<bool>? resume;
        private TaskCompletionSource<PresentationCompletion>? presentation;

        internal GameState State { get; private set; } = new(id);

        // すべてのメンバーへのアクセスは呼び出し元のEscapeAccess.gateで保護します。
        internal Task<GameCommand> BeginInputWait(CancellationToken token)
        {
            if (input is not null)
                throw new InvalidOperationException("Only one input wait is supported.");

            input = new(TaskCreationOptions.RunContinuationsAsynchronously);
            inputToken = token;
            State = State with { WaitingForInput = true };

            return input.Task;
        }

        internal void EndInputWait()
        {
            // 次の待機はinputがnullになるまで登録できず、この待機を置き換える経路もありません。
            input = null;
            inputToken = default;

            if (State.WaitingForInput)
                State = State with { WaitingForInput = false };
        }

        internal bool TrySubmitCommand(GameCommand command)
        {
            if (State.Mode != ExecutionMode.Active || command == GameCommand.Stop
                || input is not { Task.IsCompleted: false } || inputToken.IsCancellationRequested)
                return false;

            var accepted = input.TrySetResult(command);
            if (accepted)
                State = State with { WaitingForInput = false };

            return accepted;
        }

        internal Task<bool>? GetResumeTask()
            => State.Mode == ExecutionMode.Paused ? resume!.Task : null;

        internal Task<PresentationCompletion>? GetPresentationTask(PresentationKey key)
            => State.Event?.Key == key ? presentation?.Task : null;

        internal bool TryCompletePresentation(PresentationKey key)
        {
            if (State.Mode != ExecutionMode.Active || State.Event?.Key != key
                || presentation?.TrySetResult(PresentationCompletion.Completed) != true)
                return false;

            State = State with { PresentationCompleted = true };

            return true;
        }

        internal void StopSession()
        {
            State = State with
            {
                Mode = ExecutionMode.Stopping,
                Stage = GameStage.Stopping,
                WaitingForInput = false,
                ControlRevision = State.ControlRevision + 1
            };

            input?.TrySetResult(GameCommand.Stop);
            resume?.TrySetResult(false);
            presentation?.TrySetResult(PresentationCompletion.Stopped);
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
                presentation?.TrySetResult(PresentationCompletion.Stopped);
                presentation = next.Event is null
                    ? null
                    : new(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            if (previous.Mode != next.Mode)
            {
                if (next.Mode == ExecutionMode.Paused)
                    resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
                else
                {
                    resume?.TrySetResult(next.Mode == ExecutionMode.Active);
                    resume = null;
                }
            }

            if (next.Mode == ExecutionMode.Stopping)
                StopSession();

            return change with { State = State };
        }
    }
}
