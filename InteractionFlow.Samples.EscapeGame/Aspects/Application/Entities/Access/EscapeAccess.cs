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

    /// <summary>
    /// 一つのセッションの不変状態と、入力・再開・提示完了の通知先をまとめて管理します。
    /// </summary>
    /// <remarks>
    /// 状態の読み取りを含むすべてのアクセスは、呼び出し元のEscapeAccess.gateで保護します。
    /// 返されたTaskの待機はロック外で行います。
    /// </remarks>
    private sealed class SessionRuntime(long id)
    {
        private readonly InputWait input = new();
        private readonly ResumeWait resume = new();
        private readonly PresentationWait presentation = new();

        /// <summary>
        /// 現在の不変ゲーム状態を取得します。状態参照の差し替えはこのSessionRuntime内だけで行います。
        /// </summary>
        internal GameState State { get; private set; } = new(id);

        /// <summary>
        /// 入力受信先を一件登録し、配送時の取消判定用Tokenと入力待機の表示状態を設定してTaskを返します。
        /// 待機先が残っている場合は例外とし、Taskの待機と取消の処理は呼び出し元が行います。
        /// </summary>
        internal Task<GameCommand> BeginInputWait(CancellationToken token)
        {
            var task = input.Begin(token);
            State = State with { WaitingForInput = true };

            return task;
        }

        /// <summary>
        /// 入力の受信・取消・停止後のfinallyから呼び出し、受信先・Token・入力待機の表示状態を解除します。
        /// 解除が終わるまでは次の入力待機を登録できません。
        /// </summary>
        internal void EndInputWait()
        {
            input.End();

            if (State.WaitingForInput)
                State = State with { WaitingForInput = false };
        }

        /// <summary>
        /// Activeかつ未完了・取消前の入力待機へ一件配送し、成功時に入力待機の表示状態を解除します。
        /// Stop要求や配送不能な入力はfalseで拒否し、ゲーム規則上の妥当性は判断しません。
        /// </summary>
        internal bool TrySubmitCommand(GameCommand command)
        {
            if (State.Mode != ExecutionMode.Active || !input.TrySubmit(command))
                return false;

            State = State with { WaitingForInput = false };

            return true;
        }

        /// <summary>
        /// ポーズ中なら再開でtrue、停止等でfalseとなる通知Taskを返し、それ以外のモードではnullを返します。
        /// 呼び出し元はロック外で待機し、通知後に進行可能な状態かどうかを再確認します。
        /// </summary>
        internal Task<bool>? GetResumeTask()
            => State.Mode == ExecutionMode.Paused ? resume.Task : null;

        /// <summary>
        /// 現在のイベントと指定キーが一致する場合に、提示完了または停止を通知するTaskを返します。
        /// キー不一致や通知先がない場合はnull、既に完了していればその完了済みTaskを返します。
        /// </summary>
        internal Task<PresentationCompletion>? GetPresentationTask(PresentationKey key)
            => presentation.GetTask(key);

        /// <summary>
        /// Activeな現行イベントに一致する未完了の提示へ完了を通知し、状態へ完了受領を反映します。
        /// ポーズ中・停止済み・キー不一致・通知済みの場合はfalseを返します。
        /// </summary>
        internal bool TryCompletePresentation(PresentationKey key)
        {
            if (State.Mode != ExecutionMode.Active || !presentation.TryComplete(key))
                return false;

            State = State with { PresentationCompleted = true };

            return true;
        }

        /// <summary>
        /// モードと段階をStoppingにし、入力待機の表示を解除して制御変更の版を進めます。
        /// 入力にはStop、再開にはfalse、提示にはStoppedを通知し、残っている待機を解除します。
        /// </summary>
        internal void StopSession()
        {
            State = State with
            {
                Mode = ExecutionMode.Stopping,
                Stage = GameStage.Stopping,
                WaitingForInput = false,
                ControlRevision = State.ControlRevision + 1
            };

            input.Stop();
            resume.Stop();
            presentation.Stop();
        }

        /// <summary>
        /// セッションとイベントのIDを検証し、次状態のVersionを直前の版に一つ加えた値へ揃えて反映します。
        /// 提示の置換とモード変更に応じて通知先を更新し、Stoppingへの変更では停止通知も行います。
        /// 引数のStatusを保ったまま、反映後の状態を含む更新結果を返します。
        /// </summary>
        internal GameUpdateResult Commit(GameUpdateResult change)
        {
            var next = change.State ?? throw new InvalidOperationException("An update must return game state.");

            if (next.SessionId != State.SessionId || next.Event is { } e && e.Key.SessionId != next.SessionId)
                throw new InvalidOperationException("An update cannot change its session identity.");

            var previous = State;

            if (next.Version != previous.Version + 1)
                next = next with { Version = previous.Version + 1 };

            State = next;

            presentation.SetEvent(next.Event?.Key);

            if (previous.Mode != next.Mode)
                resume.ChangeMode(next.Mode);

            if (next.Mode == ExecutionMode.Stopping)
                StopSession();

            return change with { State = State };
        }
    }

    /// <summary>
    /// 入力受信先と配送時の取消判定用Tokenを管理します。アクセスはEscapeAccess.gate内で行います。
    /// </summary>
    private sealed class InputWait
    {
        private TaskCompletionSource<GameCommand>? source;
        private CancellationToken token;

        /// <summary>
        /// 一件の入力待機を登録します。完了済みでもEndによる解除までは次の登録を拒否します。
        /// </summary>
        internal Task<GameCommand> Begin(CancellationToken cancellationToken)
        {
            if (source is not null)
                throw new InvalidOperationException("Only one input wait is supported.");

            source = new(TaskCreationOptions.RunContinuationsAsynchronously);
            token = cancellationToken;

            return source.Task;
        }

        /// <summary>
        /// 登録した待機自身のfinallyから呼び出し、受信先とTokenを解除して次の登録を可能にします。
        /// </summary>
        internal void End()
        {
            source = null;
            token = default;
        }

        /// <summary>
        /// 未完了かつ取消前の受信先へ通常入力を配送します。Stop要求は拒否します。
        /// </summary>
        internal bool TrySubmit(GameCommand command)
            => command != GameCommand.Stop
                && source is { Task.IsCompleted: false }
                && !token.IsCancellationRequested
                && source.TrySetResult(command);

        /// <summary>
        /// 残っている入力待機へStopを通知します。受信先の解除はEndに任せます。
        /// </summary>
        internal void Stop() => source?.TrySetResult(GameCommand.Stop);
    }

    /// <summary>
    /// ポーズからの再開・停止通知を管理します。アクセスはEscapeAccess.gate内で行います。
    /// </summary>
    private sealed class ResumeWait
    {
        private TaskCompletionSource<bool>? source;

        /// <summary>
        /// ポーズ開始時に作成した通知Taskを取得します。ポーズ中にだけ参照します。
        /// </summary>
        internal Task<bool> Task => source!.Task;

        /// <summary>
        /// モード変更時に呼び出し、ポーズでは待機を作成し、解除時はActiveかどうかを通知して解除します。
        /// </summary>
        internal void ChangeMode(ExecutionMode mode)
        {
            if (mode == ExecutionMode.Paused)
                source = new(TaskCreationOptions.RunContinuationsAsynchronously);
            else
            {
                source?.TrySetResult(mode == ExecutionMode.Active);
                source = null;
            }
        }

        /// <summary>
        /// 残っている再開待機へfalseを通知し、進行を打ち切ります。
        /// </summary>
        internal void Stop() => source?.TrySetResult(false);
    }

    /// <summary>
    /// 提示キーに対応する完了・停止通知を管理します。アクセスはEscapeAccess.gate内で行います。
    /// </summary>
    private sealed class PresentationWait
    {
        private PresentationKey? key;
        private TaskCompletionSource<PresentationCompletion>? source;

        /// <summary>
        /// 提示キーが変わった場合に旧待機へ停止を通知し、新しい提示の待機を作成します。
        /// キーがnullなら待機を解除し、同じキーなら完了状態を含めて現在の待機を保ちます。
        /// </summary>
        internal void SetEvent(PresentationKey? nextKey)
        {
            if (key == nextKey)
                return;

            Stop();
            key = nextKey;
            source = nextKey is null ? null : new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// キーが現在の提示と一致する場合に通知Taskを返します。完了済みのTaskもそのまま返します。
        /// </summary>
        internal Task<PresentationCompletion>? GetTask(PresentationKey requestedKey)
            => key == requestedKey ? source?.Task : null;

        /// <summary>
        /// キーが一致する未完了の提示に完了を通知し、受理できた場合だけtrueを返します。
        /// </summary>
        internal bool TryComplete(PresentationKey requestedKey)
            => key == requestedKey && source?.TrySetResult(PresentationCompletion.Completed) == true;

        /// <summary>
        /// 残っている提示待機へ停止を通知します。現在のキーと通知Taskは保持します。
        /// </summary>
        internal void Stop() => source?.TrySetResult(PresentationCompletion.Stopped);
    }
}
