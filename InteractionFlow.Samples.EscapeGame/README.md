# Turn-based escape

一部屋のターン制脱出ゲームで、関心別の状態、逐次・並行フローの合成、提示完了待機、
プレイ中のポーズ・再開、リトライと終了合流を検証するサンプルです。
設計の前提は [仕様書](../docs/specs/TURN_BASED_ESCAPE_GAME.md) を参照してください。

```powershell
dotnet run --project InteractionFlow.Samples.EscapeGame
```

デスクトップの描画環境が必要です。画面はraylibの標準フォントに合わせて英語で表示します。

## 遊び方

鍵を拾い、スイッチを作動させ、扉を解錠して出口へ進みます。
有効な行動の後に敵が一マス巡回し、同じマスへ入ると敗北します。

| 操作 | 入力 |
| --- | --- |
| 移動を仮選択 | 矢印 / WASD |
| 隣接するスイッチ・扉を仮選択 | E / Interact |
| 待機を仮選択 | Space / Wait |
| 行動を確定 | Enter / Confirm |
| 仮選択を取消 | Esc / Cancel |
| ポーズ・再開 | P / Pause・Resume |
| 現在の演出を省略 | F / Skip |
| 結果画面からリトライ | R / Retry |
| 終了 | Exit / ウィンドウの閉じる操作 |

ポーズ中は盤面・選択・ゲーム演出の時間を保ち、再開と終了だけを受け付けます。
演出省略は確定済みのゲーム結果を巻き戻しません。処理中の通常操作を次ターンへ蓄積しません。

## 構成の読み方

| 分類 | 責務 |
| --- | --- |
| View | ウィンドウの生成・描画・閉鎖を扱う。ゲーム状態から表示・演出時間を作り、操作と描画後の完了を入力として送る |
| Gameplay | 入力の意味を解釈し、ゲーム規則を適用して次のゲーム状態を作る |
| Application | 入力とゲーム状態の共有、排他、一括反映、待機・通知、セッションの寿命を管理する |
| Shared | Game・Sessionに分類した共有データ、GameInfo<T>、入力内容、Context・Access・Port契約 |
| Lifetime | 共通Context基底、全体停止、終了結果の集約と例外・キャンセル時の停止方針 |

Aspectsは範囲→レイヤーの順です。共有契約以外は、各Aspect内と基底Lifetimeへの依存に限定します。
配置表は[名前空間と配置](../docs/design-notes/ESCAPE_GAME_NAMESPACE_LAYOUT.md)を参照してください。
各AspectのProfileは自身のFunction・Interactionを登録し、EscapeGameProfileがApplyします。
ゲーム進行のSystemFlowはEscapeGameProfile自身で登録します。
IWindowReactionとOpenWindow / PresentFrame / CloseWindowはViewにまとめ、ViewProfileが登録します。
LifetimeProfileはIShutdownReaction / ShutdownReaction、RequestShutdown、例外・キャンセルHandlerを登録します。
ウィンドウ閉鎖もRaylibWindowReactionが担います。

入力待機はGameplayのIGameInputOperation / GameInputOperation、入力配送はViewの
IGameInputSubmissionOperation / GameInputSubmissionOperationが担当します。
セッション終了はGameplayのIGameSessionReaction / GameSessionReactionへ分けています。
各Profileが自身のPort・実装を登録し、Sharedには共通の状態・Access・Context契約を残します。
ApplicationProfileとSharedProfileは構成の入口として維持しますが、現在はサービス登録を持ちません。

## 入力・ゲーム状態・表示の流れ

```text
Viewの入力取得 → EscapeAccessに操作を渡す → Gameplayが入力を読む
GameplayのWorldRules → GameInfo<GameChangeInfo> → EscapeAccessが一括反映
EscapeAccessのGameInfo<GameViewInfo> → RaylibWindowReaction（表示の記憶・変換）→ 描画
```

[GameState](./Aspects/Shared/Entities/Game/GameState.cs)は不変のrecordです。
取得した状態はロック外でも安全に読め、後からゲームが進んでも変化しません。
[SessionState](./Aspects/Application/Entities/SessionState.cs)はFeedback、入力待機、提示、実行モード、版を保持します。
[EscapeAccess](./Aspects/Application/Entities/Access/EscapeAccess.cs)は同時点のGameStateと必要な値を
SharedのGameInfo<T>で返し、SessionStateの実装型を外側へ公開しません。
View向けのGameInfo<GameViewInfo>は状態変更時に作成して再利用し、取得時には割り当てません。
表示文字列や演出時間はViewだけが保持します。

[GameplayReaction](./Aspects/Gameplay/Externals/GameplayReaction.cs)は、例えば次のように更新します。

```csharp
var change = context.Gameplay.UpdateGameState(context.SessionId,
    (state, _, _, _) => WorldRules.ApplyPlayer(state));
```

更新関数はロック内でGameState、実行モード、提示キー、提示完了を個別の引数で受け取ります。
WorldRulesへは必要な引数だけを渡し、GameInfo<GameChangeInfo>で次状態とセッションへの反映指示を返します。
Feedbackと提示は保持・設定・解除を区別し、SessionRuntimeが版の更新と提示キー発行を担います。
Gameplayの状態取得はGameInfo<ExecutionMode>を返します。
[WorldRules](./Aspects/Gameplay/Entities/WorldRules.cs)はゲーム規則だけを扱い、外部I/Oや待機を行いません。
更新関数が例外を送出した場合は、返却前の状態を公開しません。同期処理は短く保ちます。
IGameplayRules、受付条件のビット集合、入力待機ID、ルールのDI登録はありません。

入力は、待機中の通常入力一件と、最新の制御要求を扱います。
通常入力の待機がない間に送られた操作は配送せず、次ターンへ溜めません。
入力のゲーム上の有効性はGameplayで判断します。Viewのボタン表示は受理を保証しません。
セッションIDが異なる古い入力・制御要求は次セッションへ適用しません。

## Viewでの変換

[RaylibWindowReaction](./Aspects/View/Externals/RaylibWindowReaction.cs)がGameInfo<GameViewInfo>を読み、
GameEvent・選択・確定状態などから通常の表示文言・位置・ボタン状態を導きます。
Feedbackは取消・拒否など、現在の状態だけでは復元できない4種類の操作結果に限定しています。
操作結果はポーズや入力待機・提示完了通知を通じて保持し、次の有効操作や提示開始で解除します。
表示用DTOを毎フレーム生成せず、同時点のGameStateとGameViewInfoをまとめた取得結果を再利用します。
文言は取得結果の参照が変わったときに組み立て、同じ文言の折り返し結果も再利用します。

[PresentationClock](./Aspects/View/Entities/PresentationClock.cs)はReaction内部で演出時間を保持するヘルパーです。
更新ごとにSnapshotを返さず、自身の経過時間・所要時間・完了状態を更新します。
移動は0.15秒、スイッチ・解錠は0.2秒といった指定はView側にあります。
共有GameEventは何が起きたかと位置・識別子だけを持ち、秒数や表示文言を含みません。
ポーズ中は時計を進めず、ポーズ・再開の版を跨いだdeltaは破棄します。

[RaylibWindowReaction](./Aspects/View/Externals/RaylibWindowReaction.cs)は描画を提出してから提示完了を通知します。
EscapeAccessは現行イベントの完了通知を待機へ配送し、時間や描画内容の妥当性は判断しません。
実際に画面へ提出したことはViewの責務です。通知だけでユーザーの認識まで保証するものではありません。

## SystemFlowとポーズ

WindowSystemFlowはメインスレッドのフレーム反復と、背景のGamePlaySystemFlowの寿命を所有します。
GamePlaySystemFlowはセッションの生成・BeginSession・EndSession・リトライを管理します。
GameSessionSystemFlowはPlayTurnのターン反復とHandleResultの結果確認を合成します。
PlayTurnは選択開始・入力反復・プレイヤー行動・敵行動の提示完了までを一つのInteractionとして扱い、
HandleResultは結果提示・受付開始・リトライ入力までを扱います。
段階間とフロー間の停止・再開確認はGameplay内のGameplayCheckpointで共有します。

GamePlaySystemFlowは各実行の終了結果を読み、明示的な分岐でセッション進行・終了・リトライを判断します。
PlayTurn / HandleResultは各Reaction後に継続を明示的に判定し、失敗・正常停止ではそのReactionEndを直接返します。
SystemFlowと両InteractionのResultチェーン化は、記述量と終了処理の負担に見合う利点が得られず撤回しました。
WindowSystemFlowはtry/catch/finallyとFailureCollectorで停止・合流・閉鎖と失敗の保持を調整します。
OpenからCloseまで呼び出し元スレッドを維持します。
局所検証と適用判断の詳細は[Result APIの利用調査と実装結果](../docs/design-notes/ESCAPE_GAME_RESULT_API_REVIEW.md)を参照してください。

ポーズ・省略要求はPresentFrameが入力として送り、GameplayのHandleControlInputが解釈します。
WindowSystemFlowがフレームごとにこの独立した制御経路を実行するため、
通常進行が入力・提示・再開を待っていても制御要求を処理できます。追加の背景Taskは作りません。
制御用GameplayContextは独立Cancellationを使い、通常進行のキャンセルやリセットと干渉させません。

ポーズの可否とModeの変更はWorldRules、変更後の待機通知はEscapeAccessが担当します。
確定済みのゲーム状態は巻き戻さず、再開後に提示や処理の続きを行います。

## Contextと終了処理

EscapeApplicationContextが共有EscapeAccessを生成します。
GameplayContext / ViewContextは同じアクセス管理を使い、公開契約と探索範囲を限定します。
LifetimeContextはLifetimeの保持・取得・参照解放を共通化し、共有値の所有権を取得しません。
ContextInheritanceで親の値探索とCancellation共有を独立に指定します。
固定AccessとEntryによる差し替えはLifetimeContextの共通検証で拒否します。

更新中のゲーム状態、表示変換、Taskの起動や合流をContext自身の責務にはしません。
停止・終了では入力・提示・再開待機を解除し、ゲーム実行を合流してからCloseへ進みます。
CleanupInfoで先行結果と終了種別を渡し、終了処理自身の失敗を失わない構成を維持しています。

## 検証

今回の見直しではEscapeGame151件と他470件の全621テストを確認しています。
開始・結果提示・入力受付の失敗Resultについて、後続の打ち切りと元の失敗結果の保持を確認しています。
範囲・不足への対応・維持した重複の理由は[テスト点検](../docs/design-notes/ESCAPE_GAME_TEST_COVERAGE.md)を参照してください。
ゲームの解法・勝敗・リトライ、ポーズ・キャンセル・終了の競合、古い提示への通知に加え、
不変Snapshot、並行更新の取りこぼし防止、失敗した更新の非公開、Viewでの表示変換・時計を検証します。
旧案固有の受付ポリシー・待機IDテストは廃止し、現在のデータ境界を検証するテストへ置き換えました。

分離前の実画面確認では、W/Dによる選択、Enter確定、ターン進行、ポーズ・Pでの再開、敗北表示、Rでのリトライ、Exit終了を確認しました。
分離後は描画APIを置き換えたフロー実行とViewの状態変換・時計のテストで同じ進行を検証しています。

.NET 10.0.14 / x64、TieredCompilation=0、20,000回ウォームアップ、200,000回×3の描画なし簡易計測は次のとおりです。

| 対象 | 割り当て | 時間の目安 |
| --- | ---: | ---: |
| View向け不変状態の取得 | 0B | 約14〜15ns |
| 直接フレーム（取得2回・ローカル時計） | 0B | 約101〜103ns |
| PresentFrame一回 | 232B | 約204〜212ns |

表示DTOがあった版の直接フレーム128B / PresentFrame360Bから、それぞれ128B減りました。
計測用ReactionはGameInfo<GameViewInfo>の保持と時計更新だけを行い、実描画・文言組み立て・折り返しを含みません。
実描画時の補間文字列や、ゲーム更新・入力通知時の不変状態生成には割り当てがあります。
上の計測はネイティブ描画、ゲーム更新、入力・制御の競合を含むゲーム全体の性能保証ではありません。
再実行は[計測プロジェクト](../benchmarks/InteractionFlow.ExecutionMeasurements/README.md)を参照してください。
設計上の境界と変更理由は[ゲーム状態と入力の共有](../docs/design-notes/ESCAPE_GAME_GAMEPLAY_RULES.md)に記録しています。

過去の改修経緯と検証記録は[アーカイブ](../archives/escape-game/README.md)へ移しました。
