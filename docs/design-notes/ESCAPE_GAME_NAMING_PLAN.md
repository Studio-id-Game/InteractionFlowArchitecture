# EscapeGameの命名整理プラン

状態: 段階1〜3を実施・検証済み。
基準はContext契約分割を終えた763cf02。対象はEscapeGame、そのテスト・計測・説明文書。
Core / Standardの公開APIと他サンプルは対象外。サブエージェントは使用しない。

## 方針

- 呼び出し元から対象と目的が分かる名前にする。特に広いAccess契約のメソッドでは対象を省略しない。
- Interactionはユーザーとの一回の相互作用、Accessは状態操作・通知・待機を表す。同じ名前を機械的に揃えない。
- 非同期メソッドはAsync、受付拒否をboolで返す要求はTryを維持・補足する。
- 開始、実際の描画、完了通知、完了待機を区別する。待つだけのメソッドをPresentと呼ばない。
- 一つの名前に全副作用を詰め込まない。受付開始まで等の完了範囲はコメントと契約文書にも残す。
- 現在のGameplay / View / Lifetimeの分類、Context契約、実行手順は変更しない。
- 単語の統一だけを目的に、十分明確な名前まで置き換えない。

## 調査で見つかった主な曖昧さ

| 現在の名前 | 実際の役割と問題 |
| --- | --- |
| GameSystemFlow | セッションを実行し、リトライ時に新セッションを生成するループ。SessionSystemFlowとの差が名前から弱い |
| SelectAction / GameReaction.SelectAsync | 選択だけでなく取消・確定・結果画面のリトライ入力も処理する |
| EnableSelection / EnterSelecting | 単一操作の有効化ではなく、提示終了を確認して行動選択段階へ移る |
| CreateSession / BeginSession | 前者は未公開のID・初期状態割り当て、後者は公開と初期提示開始。違いを強調したい |
| GameReaction.PresentAsync | 描画せず、提示完了を待ってから再開待機するprivate関数 |
| View.Advance / TryComplete / TrySkip | 入力・制御・表示を統合したViewの何を操作するか不明瞭。TrySkipは即完了せず要求を立てる |
| ChangeControl / GameControlReaction.Change | ポーズ・再開・省略の入力要求を処理する。Control自体を差し替えるようにも読める |
| GameOperation | 実装内容はコマンド入力の待機と配送だけ |
| SessionReaction | セッション終了だけでなくアプリ全体の停止も担当する |
| GameFlowCheckpoint.CanContinueAsync | 単なる判定ではなく、ポーズ中に待機する |

## 段階1：呼び出し経路で効果の大きい名前を変更

以下を最初の採用案とする。複数候補を実装時に使い分けず、この対応表を基本に進める。

### クラス・Port・ファイル

| 現在 | 提案 | 理由 |
| --- | --- | --- |
| GameSystemFlow | SessionLoopSystemFlow | セッションの生成・終了・リトライ反復を担当することが分かる |
| GameOperation / IGameOperation | GameInputOperation / IGameInputOperation | 入力待機とコマンド配送の機能であることを示す |
| GameReaction / IGameReaction | GameplayReaction / IGameplayReaction | Gameplay Accessと同じゲーム実行側の応答であることを示す |
| SessionReaction / ISessionReaction | GameLifetimeReaction / IGameLifetimeReaction | セッション終了と全体停止の両方を扱うことを表す |
| SelectAction | HandleGameInput | 選択・確定・取消・リトライのいずれにも使う実態に合わせる |
| EnableSelection | BeginActionSelection | 選択可能な段階を開始するInteractionであることを示す |
| ChangeControl | HandleControlInput | フレームの制御入力への応答であり、設定オブジェクトの変更ではない |
| ShowResult | PresentResult | 結果の提示完了から入力受付開始までの相互作用を示す |
| EscapeAccess.World.cs | EscapeAccess.Gameplay.cs | 廃止したWorld契約の分類名をファイル名に残さない |

クラス・Portの改名は宣言、ファイル名、コンストラクター名、Builder登録、テストfake、文書リンクを一緒に更新する。
EscapeAccessのpartialクラス名はそのまま。内部ファイルを公開3契約に合わせて再分割する作業は含めない。

### メソッド

| 所属 | 現在 | 提案 | 意味 |
| --- | --- | --- | --- |
| IGameplayAccess / EscapeAccess | CreateSession | AllocateSession | 未公開セッションのID・初期状態を割り当てる。開始・提示とは区別 |
| ゲーム入力Port / External、IGameplayAccess / EscapeAccess | ReadAsync | ReadCommandAsync | 待機して得る値がGameCommandであることを明示 |
| ゲーム入力Port / External | Submit | TrySubmitCommand | boolによる受付成功・拒否を名前へ反映 |
| IGameViewAccess / EscapeAccess | TrySubmit | TrySubmitCommand | Portと同じ入力配送を表す |
| IGameplayAccess / EscapeAccess / WorldRules | ApplySelection | ApplyInputCommand | Result段階のRetryも含む入力反映を表す |
| IGameplayAccess / EscapeAccess | ApplyPlayer / ApplyEnemy | ApplyPlayerAction / ApplyEnemyAction | 人物そのものではなく行動を適用する |
| IGameplayAccess / EscapeAccess | EnterSelecting | BeginActionSelection | 行動選択段階への遷移を明示 |
| IGameplayAccess / EscapeAccess | ShowResult | BeginResultPresentation | 結果段階へ移り提示を開始する。描画や提示完了待機は行わない |
| IGameplayAccess / EscapeAccess | WaitForPresentationAsync | WaitForPresentationCompletionAsync | 提示の存在ではなく、その完了通知を待つ |
| 両Access / EscapeAccess | GetProgress | GetSessionProgress | 対象IDのセッション進行を取得する |
| IGameViewAccess / EscapeAccess | Snapshot | CaptureSnapshot | 画面接続用の一貫したスナップショットを作る |
| IGameViewAccess / EscapeAccess | Advance | AdvancePresentation | 演出の経過時間を進める対象を明示 |
| IGameViewAccess / EscapeAccess | TryComplete | TryCompletePresentation | 提示完了を通知する対象を明示 |
| IGameViewAccess / EscapeAccess | TrySkip | TryRequestPresentationSkip | 省略要求を立てるだけで、完了通知は別途必要なことを示す |
| ゲーム応答Port / External | BeginAsync | BeginSessionAsync | BeginSession Interactionとの対応を明示 |
| ゲーム応答Port / External | EnableSelectionAsync | BeginActionSelectionAsync | 選択段階への遷移と一致させる |
| ゲーム応答Port / External | SelectAsync | HandleCommandAsync | 確定・取消・リトライも含む応答を表す |
| ゲーム応答Port / External | ApplyPlayerAsync / ApplyEnemyAsync | ApplyPlayerActionAsync / ApplyEnemyActionAsync | 対象を明示。論理更新と提示待機まで含む契約は維持 |
| ゲーム応答Port / External | ShowResultAsync | PresentResultAsync | 提示を開始し、完了を待つ応答であることを表す |
| GameControlReaction / IGameControlReaction | Change | ApplyControlRequest | FrameStateにある制御要求を適用する |
| IWindowOperation / RaylibWindowOperation | Poll | PollFrameInput | 終了要求も含む当該フレームの入力取得 |
| IWindowReaction / RaylibWindowReaction | Draw | DrawFrame | 一フレームを描画する。Openからの呼び出しも追いやすくする |
| GameplayReaction private | PresentAsync | WaitForPresentationAndResumeAsync | 描画せず、提示完了と再開を待つ。停止時は短絡する |
| GameFlowCheckpoint | CanContinueAsync | WaitToContinueAsync | 失敗・停止の判定に加え、ポーズ時に待つことを名前へ反映 |
| WindowSystemFlow private | PresentFrames | RunFrameLoop | 入力・描画・制御を繰り返す処理全体を表す |
| WindowSystemFlow private | StopAndJoin | StopAndJoinGameplay | 停止・合流する対象を明示 |

BeginSession / EndSession / RequestShutdown / TrySetPaused / WaitForActiveAsync / EnableResultInputは維持する。
AllocateSessionとBeginSessionを分ければ、公開前の割り当てと実行開始は十分区別できる。
PresentResult Interactionの入力受付開始はEnableResultInputAsyncとして残し、関数分割を変えない。

### 読み方の例

フローの関係は次のようになる。

```text
WindowSystemFlow
  ├─ PresentFrame / HandleControlInput
  └─ SessionLoopSystemFlow
       └─ SessionSystemFlow
            ├─ BeginSession
            ├─ TurnSystemFlow
            │    ├─ BeginActionSelection
            │    ├─ HandleGameInput（確定まで反復）
            │    ├─ ApplyPlayerAction
            │    └─ ApplyEnemyAction
            ├─ PresentResult
            └─ HandleGameInput（リトライ待機）
```

```csharp
// HandleGameInputの本体
var command = await input.ReadCommandAsync(context);
return await reaction.HandleCommandAsync(context, command);

// ゲーム実行側と画面側の違い
var id = context.Gameplay.AllocateSession();
var snapshot = viewContext.View.CaptureSnapshot();
var presentation = viewContext.View.AdvancePresentation(key, elapsed, revision);
viewContext.View.TryCompletePresentation(key);
```

例は改名後の読み方であり、既存処理の順序・分担は維持する。

## 段階2：データ型と内部補助の名前を整理

段階1より優先度は低いが、採用するなら以下のまとまりで変更する。
段階1の同義名を増やす目的では使わず、型名を含めて読む際の曖昧さを減らす。

| 現在 | 提案 | 理由 |
| --- | --- | --- |
| MutationStatus / MutationResult | GameUpdateStatus / GameUpdateResult | ゲーム状態の更新結果と、開始した提示の識別子であることを明示 |
| GameMutationValidation | GameUpdateValidation | 上記の更新結果型と語彙を合わせる |
| GameProgress | SessionProgress | SessionIdを指定して読む進行情報であることを明示 |
| SessionState | SessionProgressState | SessionRuntime.Progressが保持する、進行・選択・公開状態の可変Stateであることを示す |
| GameSnapshot | GameViewSnapshot | ゲーム全状態のコピーではなく、画面の表示・操作可能性のためのスナップショット |
| Finishing / FinishingKind | CleanupInfo / CleanupKind | Close・EndSession専用スコープの後始末情報であることを示す |
| Finishing.Previous | CleanupInfo.PreviousResult | 先行する終了結果を明示 |
| EscapeApplicationContext.ApplicationSessionId | NoSessionId | 0が実在するアプリセッションIDではなく、非セッションを表すことを明示 |
| EscapeAccess.Current | FindActiveSession | 指定IDの有効なセッションを探し、ない場合はnullを返す |
| EscapeAccess.CanMutate | GetUpdateStatus | boolの判定ではなくApplied / Deferred / Stoppedを返す |
| EscapeAccess.Present | BeginPresentation | 提示状態と完了通知を準備する。描画はしない |
| EscapeAccess.PresentationFinished | IsPresentationComplete | boolを返す状態判定であることを示す |
| EscapeAccess.Stop | StopSession | アプリ全体の停止フラグ設定と、個別セッションの停止を区別する |
| DisplayState.Set | SetMessage | 更新する値と、表示版を進める処理を追いやすくする |

CleanupInfoはFlowContextではない。CleanupContextやFinalizerという名前は使わない。
SessionRuntime / PresentationRuntimeはStateと待機通知の寿命を保持する実態に合っているため維持する。
ActionEffectも、勝敗を表すGameOutcomeと混同するActionOutcomeへは変更しない。

## 段階3：描画内部の小さな名前

| 現在 | 提案 |
| --- | --- |
| RaylibLayout.Button | GetButtonBounds |
| RaylibWindowOperation.ClickedButton | GetClickedButtonIndex |
| RaylibWindowOperation.Pressed | IsEitherKeyPressed |
| RaylibWindowReaction.Tile | GetTileBounds |
| RaylibWindowReaction.Center | GetTileCenter |
| RaylibWindowReaction.DrawWrapped | DrawWrappedText |

ネイティブ型・描画ライブラリへの依存は引き続きExternals側へ置く。
ボタン文言・画面表示・操作キー・演出内容は変更しない。

## 維持する名前

- IGameplayAccess / IGameViewAccess / IApplicationLifetimeAccessと、対応するGameplay / View / Lifetimeプロパティ。
- ILifetimeContext / IGameplayContext / IGameViewContext / IApplicationContext。
- GameplayContext / GameViewContext / EscapeApplicationContext。契約と具体型の対応は現在十分明確。
- WindowSystemFlow / SessionSystemFlow / TurnSystemFlow。役割の大きさと単位を維持する。
- OpenWindow / CloseWindow / PresentFrame / BeginSession / EndSession / RequestShutdown / ApplyPlayerAction / ApplyEnemyAction。
- GameControlReaction / IGameControlReaction。ポーズ・再開・省略という制御の役割は適切。
- EscapeAccess、WorldRules、RoomDefinition、WindowState、FrameState、FailureCollector。
- VerifyAccess、MarkOpened、MarkClosed等の既存の明確な状態操作。
- Coreが要求するExecuteCoreAsync / TryGetCore / DisposeCore / ValidateAdditionalValue / ForceResetMemoryState。

特にEscapeAccessをSharedGameAccess等へ変えるだけの改名や、全クラスへのEscape接頭辞追加は行わない。
フォルダー・レイヤー・Accessの分割も変更しない。

## 実装順と確認

1. 段階1のクラス・Port・ファイルを改名し、Builder登録と全シグネチャを揃える。
2. 段階1のメソッドを呼び出し先ごとに変更する。同名の別用途メソッドを一括置換しない。
3. テスト・計測・READMEの現行構成説明を更新し、改名前後の対応表へのリンクを残す。
4. 関連テスト・全体ビルド・全体テストを実行し、段階1を独立コミットにする。
5. 段階2、段階3もそれぞれ差分を分けて実施・確認する。改名と機能整理を同じ変更へ混ぜない。

検証する内容:

- C#の型・メソッド参照だけでなく、nameofを含む診断、テストfake、DI登録、文書リンクを確認する。
- リフレクションや文字列で旧型名を参照していないか検索する。
- 過去の検証・計測記録を現行の結果へ書き換えず、ファイル移動で壊れるリンクは修正する。
- 更新順・await位置・例外捕捉・ロック範囲・ContextInheritance指定・Reactionの終了生成を変えない。
- 名前変更に伴うnameofの診断表示は変わるが、例外型・成功失敗の区分・保持する原因は維持する。
- 通常PresentFrameの割り当て488B/回を基準に、Release簡易計測で増加がないことを確認する。
- 通常の名前変更だけならヘッドレス経路で確認する。描画や操作挙動を変える必要が出た場合は別変更として扱う。

## 実施結果

- 段階1: 全体ビルドは警告・エラー0件、EscapeGame99件と他プロジェクト470件、計569件が成功。
  C#66ファイルの識別子・コメント・空白以外のトークンを比較し差分なし。移動ソースへの文書リンク切れなし。
  旧名と新名の対応表は履歴・移行用として残す。
- 段階2: データ型と内部補助を改名し、全体ビルドは警告・エラー0件、全569件成功。
  C#66ファイルの識別子・コメント・空白以外のトークンに差分なし。
- 段階3: 描画ヘルパーを改名。テスト補助の提示開始待機はWaitForPresentationStartAsync、
  セッションContext構築はCreateSessionContextとし、別用途のAccessメソッドと区別した。
  全体ビルドは警告・エラー0件、全569件成功。最終の補助名調整後も関連99件成功。
- 最終状態を763cf02と比較し、C#66ファイルの識別子・コメント・空白以外のトークンに差分なし。
  移動ソースへの文書リンク切れなし。画面文字列、演算、条件、待機・更新順は維持した。
- Release簡易計測は通常PresentFrameが488B/回・約228〜233ns。割り当て量の増加なし。
  実画面での再確認は未実施。サブエージェントは使用していない。

関連: [AccessとContextの再分類](./ESCAPE_GAME_ACCESS_CONTRACT_REVIEW_PLAN.md)、
[サンプルの構成と検証](../../InteractionFlow.Samples.EscapeGame/README.md)。
