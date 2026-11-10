using InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Application.Entities;

/// <summary>
/// 一つのセッションの不変状態と、入力・再開・提示完了の通知先をまとめて管理します。
/// </summary>
/// <remarks>
/// 状態の読み取りを含むすべてのアクセスは、呼び出し元のEscapeAccess.gateで保護します。
/// 返されたTaskの待機はロック外で行います。
/// </remarks>
internal sealed class SessionRuntime
{
    private readonly InputWait input = new();
    private readonly ResumeWait resume = new();
    private readonly PresentationWait presentation = new();
    private SessionState session = new();

    /// <summary> 未公開セッションのゲーム状態とセッション状態、View向けの初期取得結果を作成します。 </summary>
    internal SessionRuntime(long id)
    {
        State = new(id);
        View = CreateView();
    }

    /// <summary> 現在の不変ゲーム状態を取得します。差し替えはSessionRuntime内だけで行います。 </summary>
    internal GameState State { get; private set; }

    /// <summary> セッションの実行モードを取得し、公開・受付・再開待機の判定に使います。 </summary>
    internal ExecutionMode Mode => session.Mode;

    /// <summary> 同時点のゲーム状態とView向け情報を返します。取得時には再生成しません。 </summary>
    internal GameInfo<GameViewInfo> View { get; private set; }

    /// <summary>
    /// ロック内の現行状態と規則が必要とする値を引数へ分解して渡し、戻された次状態と指示を反映します。
    /// </summary>
    internal GameUpdateResult Update(
        Func<GameState, ExecutionMode, PresentationKey?, bool, GameInfo<GameChangeInfo>> update)
        => Commit(update(State, session.Mode, session.Event?.Key, session.PresentationCompleted));

    /// <summary>
    /// 一件の入力受信先と取消判定用Tokenを登録し、受付表示を更新してTaskを返します。
    /// 解除前の二重登録は拒否します。Taskの待機と取消はロック外で行います。
    /// </summary>
    internal Task<GameCommand> BeginInputWait(CancellationToken token)
    {
        var task = input.Begin(token);
        session = session with { WaitingForInput = true };
        View = CreateView();

        return task;
    }

    /// <summary>
    /// 入力受信・取消・停止後のfinallyから呼び出し、受信先と受付表示を解除します。
    /// 受信先はこの処理まで保持するため、次の入力待機との同一性確認は不要です。
    /// </summary>
    internal void EndInputWait()
    {
        input.End();

        if (session.WaitingForInput)
        {
            session = session with { WaitingForInput = false };
            View = CreateView();
        }
    }

    /// <summary>
    /// Activeかつ未完了・取消前の受信先へ通常入力を配送し、成功時に受付表示を解除します。
    /// ゲーム規則上の妥当性は判断しません。
    /// </summary>
    internal bool TrySubmitCommand(GameCommand command)
    {
        if (Mode != ExecutionMode.Active || !input.TrySubmit(command))
            return false;

        session = session with { WaitingForInput = false };
        View = CreateView();

        return true;
    }

    /// <summary>
    /// ポーズ中なら再開でtrue、停止でfalseとなる通知Taskを返し、他のモードではnullを返します。
    /// 呼び出し元はロック外で待機し、通知後に実行モードを再確認します。
    /// </summary>
    internal Task<bool>? GetResumeTask()
        => Mode == ExecutionMode.Paused ? resume.Task : null;

    /// <summary>
    /// 現在の提示キーに対応する完了・停止通知Taskを返し、不一致ならnullを返します。
    /// 完了済みの場合も同じTaskを返します。
    /// </summary>
    internal Task<PresentationCompletion>? GetPresentationTask(PresentationKey key)
        => presentation.GetTask(key);

    /// <summary>
    /// Activeな現行提示に一致する未完了の通知を受理し、規則評価へ渡す完了状態を更新します。
    /// ポーズ中・停止済み・キー不一致・通知済みならfalseを返します。
    /// </summary>
    internal bool TryCompletePresentation(PresentationKey key)
    {
        if (Mode != ExecutionMode.Active || !presentation.TryComplete(key))
            return false;

        session = session with { PresentationCompleted = true };

        return true;
    }

    /// <summary>
    /// 実行モードとゲーム段階をStoppingにし、受付表示を解除して制御版を進めます。
    /// 入力へStop、再開へfalse、提示へStoppedを通知して残っている待機を解除します。
    /// </summary>
    internal void StopSession()
    {
        State = State with { Stage = GameStage.Stopping };
        session = session with
        {
            Mode = ExecutionMode.Stopping,
            WaitingForInput = false,
            ControlRevision = session.ControlRevision + 1
        };
        View = CreateView();

        input.Stop();
        resume.Stop();
        presentation.Stop();
    }

    /// <summary>
    /// 次状態のセッションIDを検証し、更新版を進めて状態とセッションへの指示を一括反映します。
    /// 提示キーを発行し、モード変更に伴う通知先を更新します。停止指示では停止通知も行います。
    /// 操作の成立可否を共有アクセスのStatusへ変換し、現行提示キーとともに返します。
    /// </summary>
    internal GameUpdateResult Commit(GameInfo<GameChangeInfo> change)
    {
        var next = change.State ?? throw new InvalidOperationException("An update must return game state.");

        if (next.SessionId != State.SessionId)
            throw new InvalidOperationException("An update cannot change its session identity.");

        var info = change.Info;
        var previous = session;
        var version = previous.Version + 1;
        var mode = info.Mode ?? previous.Mode;
        var nextEvent = previous.Event;

        if (info.ReplacePresentation)
            nextEvent = info.Presentation is { } kind
                ? new(new(next.SessionId, version, version), kind, info.From, info.To)
                : null;

        session = previous with
        {
            Version = version,
            Feedback = info.ReplaceFeedback ? info.Feedback : previous.Feedback,
            Mode = mode,
            ControlRevision = previous.ControlRevision
                + (mode != previous.Mode && mode != ExecutionMode.Stopping ? 1 : 0),
            Event = nextEvent,
            PresentationCompleted = info.ReplacePresentation && nextEvent is not null
                ? false : previous.PresentationCompleted,
            SkipRequested = info.ReplacePresentation && nextEvent is not null
                ? false : previous.SkipRequested || info.RequestSkip
        };
        State = next;

        presentation.SetEvent(session.Event?.Key);

        if (previous.Mode != mode)
            resume.ChangeMode(mode);

        if (mode == ExecutionMode.Stopping)
            StopSession();
        else
            View = CreateView();

        return new(info.Accepted ? GameUpdateStatus.Applied : GameUpdateStatus.Rejected, session.Event?.Key);
    }

    /// <summary>
    /// 現行のゲーム状態とViewが必要な値だけを不変の取得結果へまとめます。状態変更時にだけ作成します。
    /// </summary>
    private GameInfo<GameViewInfo> CreateView()
        => new(State, new(session.Feedback, session.Mode, session.WaitingForInput,
            session.Event, session.SkipRequested, session.ControlRevision));
}
