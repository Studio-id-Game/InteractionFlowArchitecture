using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.EscapeGame.Aspects.Shared.Entities.Access;

public interface IGameplayAccess
{
    /// <summary>
    /// 未公開のセッションを確保し、そのセッションを識別するIDを返します。
    /// 初期化と公開はBeginSessionで行い、アプリ全体の停止後は新しいセッションを確保できません。
    /// </summary>
    long AllocateSession();

    /// <summary>
    /// 初期化関数が操作の成立可否と次状態を返し、Acceptedがtrueの場合だけ状態を反映して公開します。
    /// 対象が存在しないか停止済みならStopped、停止していない公開済みセッションがある場合はRejectedを返します。
    /// </summary>
    GameUpdateResult BeginSession(long sessionId, Func<GameState, GameStateChange> initialize);

    /// <summary>
    /// 対象セッションを管理対象から取り除いて停止し、入力・再開・提示完了の待機を解除します。
    /// 対象が既に存在しない場合は何もしません。
    /// </summary>
    void EndSession(long sessionId);

    /// <summary>
    /// 指定IDの停止していない公開中セッションから、ロック外でも参照できる不変のゲーム状態を取得します。
    /// 対象が未公開・停止済み・存在しない場合は、指定IDを持つStopping状態を返します。
    /// </summary>
    GameState ReadGameState(long sessionId);

    /// <summary>
    /// 現在状態の取得から更新関数の同期実行・次状態の一括反映までを、同じ排他制御内で行います。
    /// 対象が有効でなければStopped、ポーズ中は原則Deferredを返します。
    /// 更新関数は操作の成立可否と次状態を返し、AcceptedがfalseでもFeedback等の状態を反映してRejectedを返します。
    /// <paramref name="allowWhilePaused"/>がtrueならポーズ中も更新します。結果には状態ではなく提示キーを含めます。
    /// 更新関数は短い計算に限定し、外部I/Oや待機、Accessへの再入呼び出しを行わないでください。
    /// </summary>
    GameUpdateResult UpdateGameState(long sessionId, Func<GameState, GameStateChange> update,
        bool allowWhilePaused = false);

    /// <summary>
    /// 指定セッションに通常入力の受信先を一件登録し、入力の配送を非同期に待ちます。
    /// 対象が有効でない場合や停止通知で待機を解除した場合はStopを返し、同一セッションの二重待機は拒否します。
    /// Tokenによる待機のキャンセルに対応し、終了時には受信先と入力待機の表示状態を解除します。
    /// </summary>
    ValueTask<GameCommand> ReadCommandAsync(long sessionId, CancellationToken token);

    /// <summary>
    /// 保持している最新一件の制御要求を取り出して消費し、要求がなければfalseを返します。
    /// ポーズや演出省略の適用は行わず、要求の意味の解釈はGameplay側で行います。
    /// </summary>
    bool TryReadControlInput(out GameControlInput input);

    /// <summary>
    /// 指定セッションがポーズ中なら再開を待ち、進行可能ならtrue、対象が無効または停止済みならfalseを返します。
    /// 再開通知後にも状態を再確認し、再びポーズしていれば待機を続けます。Tokenによる取消にも対応します。
    /// </summary>
    ValueTask<bool> WaitForActiveAsync(long sessionId, CancellationToken token);

    /// <summary>
    /// 指定キーの現行提示がViewから完了通知を受けるまで待ち、CompletedまたはStoppedを返します。
    /// 完了前の提示置換・セッション停止や対象不一致ではStoppedとなり、待機のキャンセルで確定済み状態は巻き戻しません。
    /// </summary>
    ValueTask<PresentationCompletion> WaitForPresentationCompletionAsync(PresentationKey key, CancellationToken token);
}
