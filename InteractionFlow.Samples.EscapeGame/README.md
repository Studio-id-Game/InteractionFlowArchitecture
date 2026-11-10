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
| View | ゲーム状態から文言・ボタン状態・表示・演出時間を作る。操作と描画後の完了を入力として送る |
| Gameplay | 入力の意味を解釈し、ゲーム規則を適用して次のゲーム状態を作る |
| Application | 入力とゲーム状態の共有、排他、一括反映、待機・通知、セッションの寿命を管理する |
| Shared | 不変のGameState、入力内容、Context・Access・Port契約と共通Handler |
| Lifetime | 共通Context基底と停止・閉鎖の相互作用 |

Aspectsは範囲→レイヤーの順です。共有契約以外は、各Aspect内と基底Lifetimeへの依存に限定します。
配置表は[名前空間と配置](../docs/design-notes/ESCAPE_GAME_NAMESPACE_LAYOUT.md)を参照してください。
各AspectのProfileは自身のFunction・Interactionを登録し、EscapeGameProfileがApplyします。
ゲーム進行のSystemFlowはEscapeGameProfile自身で登録します。

## 入力・ゲーム状態・表示の流れ

```text
Viewの入力取得 → EscapeAccessに操作を渡す → Gameplayが入力を読む
GameplayのWorldRules → 次のGameState → EscapeAccessが一括反映
EscapeAccessのGameState → RaylibWindowReaction（表示の記憶・変換）→ 描画
```

[GameState](./Aspects/Shared/Entities/GameState.cs)は不変のrecordです。
取得した状態はロック外でも安全に読め、後からゲームが進んでも変化しません。
[EscapeAccess](./Aspects/Application/Entities/Access/EscapeAccess.cs)は表示用Snapshot、文字列、演出時間を保持しません。

[GameplayReaction](./Aspects/Gameplay/Externals/GameplayReaction.cs)は、例えば次のように更新します。

```csharp
var change = context.Gameplay.UpdateGameState(context.SessionId, WorldRules.ApplyPlayer);
```

更新関数はロック内で現在のGameStateを受け取り、次の不変状態を返します。
[WorldRules](./Aspects/Gameplay/Entities/WorldRules.cs)はゲーム規則だけを扱い、外部I/Oや待機を行いません。
更新関数が例外を送出した場合は、返却前の状態を公開しません。同期処理は短く保ちます。
IGameplayRules、受付条件のビット集合、入力待機ID、ルールのDI登録はありません。

入力は、待機中の通常入力一件と、最新の制御要求を扱います。
通常入力の待機がない間に送られた操作は配送せず、次ターンへ溜めません。
入力のゲーム上の有効性はGameplayで判断します。Viewのボタン表示は受理を保証しません。
セッションIDが異なる古い入力・制御要求は次セッションへ適用しません。

## Viewでの変換

[RaylibWindowReaction](./Aspects/View/Externals/RaylibWindowReaction.cs)がGameStateを読み、
GameFeedbackやGameEventから表示文言・位置・ボタン状態を導きます。
ViewSnapshot / PresentationSnapshot / ViewProjectionは不要になり、表示用DTOを毎フレーム生成しません。
文言はゲーム状態の参照が変わったときに組み立て、同じ文言の折り返し結果も再利用します。

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
GameSessionSystemFlowはターン反復と結果提示・入力、GameTurnSystemFlowは一ターンを合成します。

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
固定AccessとEntryによる差し替えはContext固有の検証で拒否します。

更新中のゲーム状態、表示変換、Taskの起動や合流をContext自身の責務にはしません。
停止・終了では入力・提示・再開待機を解除し、ゲーム実行を合流してからCloseへ進みます。
CleanupInfoで先行結果と終了種別を渡し、終了処理自身の失敗を失わない構成を維持しています。

## 検証

全体・計測プロジェクトのビルドは警告・エラー0件、EscapeGame109件と他470件の全579テストが成功しました。
ゲームの解法・勝敗・リトライ、ポーズ・キャンセル・終了の競合、古い提示への通知に加え、
不変Snapshot、並行更新の取りこぼし防止、失敗した更新の非公開、Viewでの表示変換・時計を検証します。
旧案固有の受付ポリシー・待機IDテストは廃止し、現在のデータ境界を検証するテストへ置き換えました。

実画面でW/Dによる選択、Enter確定、ターン進行、ポーズ・Pでの再開、敗北表示、Rでのリトライ、Exit終了を確認しました。

.NET 10.0.14 / x64、TieredCompilation=0、20,000回ウォームアップ、200,000回×3の描画なし簡易計測は次のとおりです。

| 対象 | 割り当て | 時間の目安 |
| --- | ---: | ---: |
| 不変GameStateの取得 | 0B | 約14〜15ns |
| 直接フレーム（取得2回・ローカル時計） | 0B | 約99ns |
| PresentFrame一回 | 232B | 約186〜193ns |

表示DTOがあった版の直接フレーム128B / PresentFrame360Bから、それぞれ128B減りました。
計測用ReactionはGameStateの保持と時計更新だけを行い、実描画・文言組み立て・折り返しを含みません。
実描画時の補間文字列や、ゲーム更新・入力通知時の不変状態生成には割り当てがあります。
上の計測はネイティブ描画、ゲーム更新、入力・制御の競合を含むゲーム全体の性能保証ではありません。
再実行は[計測プロジェクト](../benchmarks/InteractionFlow.ExecutionMeasurements/README.md)を参照してください。
設計上の境界と変更理由は[ゲーム状態と入力の共有](../docs/design-notes/ESCAPE_GAME_GAMEPLAY_RULES.md)に記録しています。
