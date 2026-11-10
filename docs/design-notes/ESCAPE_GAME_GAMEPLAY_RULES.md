# EscapeGameのゲーム状態と入力の共有

現行の状態共有・同期契約をまとめる。旧方式の計画と移行記録は[アーカイブ](../../archives/escape-game/README.md)に保存する。

## 責務

- Viewは入力内容を渡し、取得したゲーム状態から表示を作る。
- Gameplayは入力の意味を解釈し、ゲーム規則を適用してゲーム状態を更新する。
- EscapeAccessは入力とゲーム状態の受け渡しを安全に同期する。

共有するのは不変のGameState、用途別の付帯情報と入力内容。表示文字列・演出時間はViewに置く。
IGameplayRulesのDIや実行時注入、受付ポリシーの公開、入力待機IDは廃止した。
ルールはGameplayの通常の計算であり、Applicationはその型も知る必要がない。

## 更新と取得

```csharp
var result = context.Gameplay.UpdateGameState(context.SessionId,
    (state, _, _, _) => WorldRules.ApplyPlayer(state));
var game = context.View.ReadGameState();
var shown = clock.Update(game, deltaSeconds);
```

UpdateGameStateはロック内で現在の不変状態を更新関数へ渡し、返却された次状態を一括反映する。
WorldRulesはGameInfo<GameChangeInfo>で次状態と操作の成立可否、セッションへの反映指示を返す。
更新入力はGameState、実行モード、提示キー、提示完了の個別引数で渡し、各規則へ必要な引数だけを渡す。
GameChangeInfoはFeedbackと提示の保持・設定・解除、モード変更、スキップ要求を表す。
SessionStateはApplication.Entitiesに置き、Feedback・WaitingForInput・Event・PresentationCompleted・
SkipRequested・Mode・ControlRevision・VersionをSessionRuntimeが所有する。
提示キーはSessionRuntimeが更新版から発行し、WorldRulesは種類と位置だけを返す。
WorldRulesは保留・停止等の受付判断を行わず、GameUpdateStatusは使わない。
EscapeAccessは通常更新ではInfo.Acceptedがfalseでもゲーム状態とFeedback等の指示を反映し、Rejectedへ変換する。
BeginSessionではInfo.Acceptedがtrueの場合だけ反映・公開する。
GameUpdateResultはEscapeAccessからの結果としてStatusとPresentationだけを持ち、GameState全体は返さない。
提示キーはロック内で反映した状態のGameEventから取得するため、待機対象を別の状態取得で探す必要がない。
更新関数が投げた場合、元のGameStateとSessionStateはそのまま保持する。他セッションの状態は反映しない。
イベントのIDはApplicationが発行するため、更新関数から他セッションのイベントを持ち込む経路はない。
更新関数は短い同期処理とし、外部I/O、待機、Accessの再入呼び出しや外部への副作用は行わない。
外部副作用やクロージャで別途変更した値までロールバックする仕組みではない。

GameStateは不変recordで、内部に共有可変オブジェクトを持たない。
取得済み状態は将来の更新で変化せず、そのままロック外で読める。
共有の可変Stateを呼び出し元へ貸し出す方法や、Snapshot取得時に全状態をコピーする方法は使わない。
セッションの公開・退役、入力・再開・提示完了の待機はApplication.EntitiesのinternalなSessionRuntimeへ閉じる。
SessionRuntimeは自身のCommit / StopSessionで更新と停止通知を行い、排他は呼び出し元のEscapeAccess.gateが担う。
SessionRuntimeは入力待機や提示完了に伴うSessionStateの更新を担い、通知処理は同じ名前空間のinternal classへ分ける。
InputWaitは入力受信先と取消判定用Token、ResumeWaitはポーズからの再開通知、
PresentationWaitは提示キーと完了・停止通知を管理する。各クラスはセッションごとに一つ作成する。
GameStateとSessionStateの差し替えと各通知先はprivateに閉じ、3クラスの操作も同じEscapeAccess.gateで保護する。
EscapeAccessはロック内で対象セッションを選択して処理を呼び、取得したTaskをロック外でawaitする。
SessionRuntimeはTaskCompletionSourceを外側へ公開せず、待機に必要なTaskだけを返す。
公開中のセッションはactiveが指し、停止済みかどうかはSessionState.Modeで判断する。Published / Retiredの別フラグは持たない。
停止していない公開済みセッションが存在する間はBeginSessionを拒否し、停止済みセッションの再公開も拒否する。
停止済みの除外はFindActiveSessionへ集約する。複数の未公開セッションを確保する契約は維持する。

## 入力と制御

通常入力は未完了の待機先一件へ配送し、待機先がない間は蓄積しない。
入力待機先が非nullの間は二件目の待機登録を拒否する。停止・配送・キャンセルで受信先を置換せず、
登録した待機自身のfinallyからEndInputWaitを呼んで解除するため、後始末時の受信先の同一性確認は不要とする。
後始末は登録時に取得したSessionRuntimeに対して行い、次セッションの入力先を変更しない。
EscapeAccessは対象セッション・実行モード・待機先・取消状態を確認するが、ゲーム段階や行動の妥当性は判断しない。
Retry等を受理する段階か、移動や解錠が成立するかはGameplayで判断する。
同じセッションの入力を過去の画面版へ厳密に結びつける契約は設けない。配送時の待機先で一件を受け取る。

制御入力は最新一件を保持する。Viewからの要求だけでゲーム状態は変化しない。
GameplayのHandleControlInputが制御要求を読み、WorldRulesでポーズ・省略の可否を判断する。
WindowSystemFlowが通常進行と独立してこのInteractionを実行するため、待機中も制御できる。
制御用Contextは通常進行と独立したCancellationを使う。制御用の背景Taskは追加しない。

Applicationは通常更新をPaused中にDeferredにし、制御用更新はallowWhilePausedで実行する。
実行モードが変わったら再開待機を通知し、停止では入力・再開・提示待機を全て解除する。
ゲーム上どの段階でポーズ・省略可能かは、Applicationへ戻さない。

## ゲーム状態から表示へ

InputFeedbackは取消・無効選択・使用可能な仕掛けなし・受付段階外の4種類の操作結果を表す。
SessionState.FeedbackはInputFeedback?で、通常進行時はnullとなる。
GameEventは発生した出来事と位置・識別子を表す。
表示文字列・ボタン表示はRaylibWindowReaction、秒数と経過時間はその内部のPresentationClockが決定する。
Viewへの取得結果はGameInfo<GameViewInfo>で、GameStateと必要なセッション情報を同時に返す。
状態変更時に作成した取得結果を再利用し、毎フレームの取得では割り当てない。
GameplayにはGameInfo<ExecutionMode>を返す。SessionStateの実装型をShared・Gameplay・Viewへ公開しない。
ReactionはView向け取得結果の参照・文言・折り返し結果を記憶する。

Viewは描画提出後に提示完了を入力通知として返す。
EscapeAccessは現行イベントかを確認して待機先へ配送し、SessionStateの完了受領状態を更新する。
描画を実行したことや演出時間の判断はViewの責務であり、同期側で再計算しない。
ポーズ・再開のControlRevisionを跨いだdeltaはViewの時計が破棄する。
古い提示キー・終了済みセッションへの完了通知は受理しない。

## 検証

現在の責務・契約とテストの対応、追加した境界ケースは[テスト点検](./ESCAPE_GAME_TEST_COVERAGE.md)を参照する。
描画なし計測の条件と結果は[サンプルREADME](../../InteractionFlow.Samples.EscapeGame/README.md)を参照する。

## Function内部の記憶と共有状態の切り分け

Operation / Reactionは状態を持てる。RaylibWindowReactionはGameInfo<GameViewInfo>、
表示文言、折り返し結果、演出時計を保持する。
DrawFrameは一つの不変GameInfo<GameViewInfo>から描画し、EndDrawing後にそのイベントの提示完了を通知する。
ForceResetMemoryStateは状態参照・文言・行キャッシュ・時計を初期化する。
このReactionはWindowSystemFlowの描画スレッドで逐次利用する。

他のOperation / Reactionも確認したが、状態の追加移動は行わない。

| 対象 | 判断と理由 |
| --- | --- |
| RaylibWindowOperation / FrameState | FrameStateはOperation・Interaction・SystemFlow間の受け渡し値。Operation内部へ隠さない |
| WindowState | Operation / Reactionが共用するネイティブ資源の状態・スレッド所有権。Contextに残す |
| GameInputOperation / GameInputSubmissionOperation | 入力の配送は別機能・別スレッドとの協調。内部キューや待機を持つとEscapeAccessと同期境界が重複する |
| GameplayReaction | ゲーム状態はViewやFlowも読む。入力・制御引数は呼び出し固有なので、可変フィールドへ移さない |
| GameSessionReaction / ShutdownReaction / Close | 先行結果・後始末の失敗は呼び出し単位の値。フィールドにすると再利用時の残留や並行呼び出しの混線を招く |
| 演出完了・ポーズ解除の待機 | View / Gameplay間の通知なのでEscapeAccessに残す。ローカルの演出時計とは所有者が異なる |

Function内部への集約は「そのFunctionだけで閉じる記憶」に限る。
共有状態や通知まで移すことを一律の簡素化としない。

## 進行とContextの契約

ゲーム進行はReadGameState(sessionId)で取得したGameInfo<ExecutionMode>から確認する。
awaitやInteraction実行後には必要な状態を再取得し、取得済みの状態を最新値として使い続けない。
GameplayReactionは更新結果の検証を内部にまとめ、プレイヤー行動・敵行動・結果提示で
再開待機、Deferredの再試行、提示完了待機を共有する。初回公開と制御要求の経路は個別に保つ。
LifetimeContextが固定AccessとEntryの追加を拒否し、具象Contextとその基底利用の双方に適用する。
役割別の探索制限、Cancellationの共有、共有Accessを所有しない寿命の契約は維持している。
各AspectのProfileも責務分離として維持した。
改修時の判断と検証記録は[簡素化プランの実績](../../archives/escape-game/ESCAPE_GAME_SIMPLIFICATION_PLAN.md)を参照する。

## Feedbackの保持と表示への変換

通常の文言はViewがEvent、Selection、Confirmed、RetryRequested、Outcome等から導出する。
ゲーム規則はPlayerActed・EnemyMoved・Result等の進行通知を重複保持しない。
取消・拒否の操作結果は現在状態だけから復元できないため、Feedbackに保持し、Viewはこれを優先する。
ポーズ・再開、入力待機の開始・完了、提示完了通知、更新の前提条件違反では既存の結果を消さない。
有効な選択・確定・リトライ、選択段階開始、新しいイベントの提示開始でFeedbackをnullに戻す。
新しいイベントによる解除はWorldRules.Presentでまとめて行う。
表示文字列・表示履歴・タイマーをGameplayや共有アクセスへ追加しない。

## AspectごとのPortと実装

入力待機はGameplayのIGameInputOperation / GameInputOperation、Viewからの配送は
IGameInputSubmissionOperation / GameInputSubmissionOperationへ分離した。
セッション終了はGameplayのIGameSessionReaction / GameSessionReaction、全体停止は
LifetimeのIShutdownReaction / ShutdownReactionへ分離した。
Portごとに対応するAspectのProfileが登録し、各Interactionは自身に必要な契約だけを受け取る。
入力先・セッション・提示・再開待機の共有と排他は従来どおりEscapeAccessが所有する。
独立したキュー・待機・可変状態や、Aspect間の直接呼び出しは追加していない。
ApplicationProfileとSharedProfileは構成の入口として維持し、現在は登録処理を持たない。

## ターンと結果確認のInteraction

PlayTurnは選択開始・選択入力の反復・プレイヤー行動・必要な敵行動をPortから接続する。
HandleResultは結果提示・受付開始・リトライ入力の反復を接続する。
GameSessionSystemFlowはターンを反復し、勝敗確定後に結果確認へ進む。
段階間の失敗・停止・再開確認はGameplayCheckpointが担い、ReactionEndを受け取る。
PlayTurn / HandleResultは各Reaction後の継続判定と早期returnを使う。
Resultチェーン化は記述量と終了トークン保持の負担に見合う利点がなく撤回した。
失敗・正常停止では元のReactionEndを直接返す。
反復の境界を保ち、送出例外とTaskキャンセルは既存のInteraction基底へ伝播する。
セッション確保はTryAllocateSessionで停止との競合を同じlock内で判定し、停止後はfalseとID 0を返す。
実行所有者であるSystemFlowは、返却失敗とフローまで伝播した例外を終了結果へ保持し、後始末を試みる。
SystemFlowのResultチェーン化も撤回し、実行・停止・終了の分岐とtry/catch/finallyを維持する。
WindowSystemFlowはFailureCollectorで失敗を保持し、停止・合流・閉鎖を呼び出し元スレッドで進める。
Interactionは別のInteractionを実行せず、ゲーム状態の更新は引き続きReaction / Operationが行う。
例外・キャンセルの処理と実行登録は1ターンまたは結果確認の単位となり、個々のPort呼び出しごとには行わない。
入力・提示待機の同期契約、ポーズと省略の独立した制御経路、終了時の停止・合流の順序は維持する。
