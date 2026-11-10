# EscapeGameの実装簡素化プラン

作成・実装日: 2026-11-10。状態: B〜Eと空コンストラクタ削除を実施済み。Aは利用者の方針により不採用。

## 結論

登録用Profileは責務分離を表すため維持した。
SessionProgressの廃止、Contextの追加値検証の共通化、更新結果検証の集約、
GameplayReactionの提示付き更新の共通化を実施した。
実績はクラス1個・record struct 1個・メソッド実装4個の純減となった。
共通化で増えた補助メソッド1個も差し引いた値である。
これらは初回整理の実績。後続のFeedback整理・Aspect別Port分割に続き、今回はターン・結果確認をInteractionへ集約した。

Interactionの一行委譲には例外・キャンセル処理の境界としての役割がある。
これを一律に消す方法や、全段階をenumで切り替える汎用Interactionは採用しない。
進行はSystemFlow、規則はGameplay、描画・表示変換はView、共有と同期はApplicationに維持する。

## 調査時の対象と基準

- 対象: InteractionFlow.Samples.EscapeGame.csprojと、その配下の手書きC#全66ファイル。
- 関連確認: EscapeGame.Tests、フレーム計測コード、CoreのInteraction基底、Standardの登録API。
- 初回調査は当時の未コミット変更を含む作業ツリーを基準にした。実装開始時はクリーンであることを確認した。
- 調査時規模: 2,407行、クラス46、interface 12、record/record struct 9、enum 11、合計78型。
- 集計はbin/objを除き、partial classは同一型として数えた。生成メンバーは含めない。
- 既存テストの再実行: 合格109、失敗0、スキップ0。依存プロジェクトを含むビルドも成功した。
- 調査時は実画面操作、全ソリューションのテスト、性能計測を実施していない。実装後の結果は末尾に記載する。

プロジェクトファイル自体は、net10.0・Raylib-cs・Standard・Analyzerとテスト向け内部公開だけで、
冗長な構成は見当たらない。主な改善対象はC#側の分割粒度である。

## 維持する構造と流れ

ここでいうMVVM的構造は、厳密なWPF式のViewModelクラス追加ではなく、
ゲームの規則・状態と、操作の解釈・進行と、表示・描画を分ける現在の構造を指す。

| 責務 | 現在の担当 | 計画後 |
| --- | --- | --- |
| ゲーム状態・規則 | GameState、WorldRules | 維持 |
| 操作と反応の接続 | Interaction、Operation/Reaction | 維持 |
| 表示値の導出・演出・描画 | RaylibWindowReaction、PresentationClock | 維持 |
| 状態の一括反映・待機と通知 | EscapeAccess | 維持 |
| 状態とアクセスの提供 | 役割別Context/Access契約 | 維持 |
| 進行・並行実行・終了合流 | SystemFlow | 維持 |

読み進める順序は次のままにする。

```text
Program → EscapeGameProfile → WindowSystemFlow
  ├─ フレーム: PresentFrame → 入力配送・描画 → HandleControlInput
  └─ ゲーム: GamePlaySystemFlow → BeginSession
       → GameSessionSystemFlow
           → PlayTurn（選択開始 → 入力反復 → プレイヤー行動 → 敵行動）を反復
           → HandleResult（結果提示 → 受付開始 → リトライ入力）
       → EndSession → 次セッションまたは終了
  → 停止要求 → ゲーム合流 → CloseWindow
```

## 削減候補

### A. 登録用Profileの集約【不採用】

各AspectのProfileは責務分離を表すため、利用者の指示に従い維持した。
EscapeGameProfileも各ProfileをApplyする現在の構成を保つ。
この案で見込んだクラス5個・Configure 5個の削減は実績に含めない。

### B. SessionProgressをGameStateの直接参照へ置き換える【実施済み】

対象: [ゲームの共有値](../../InteractionFlow.Samples.EscapeGame/Aspects/Shared/Entities/Game/)・[セッションの共有値](../../InteractionFlow.Samples.EscapeGame/Aspects/Shared/Entities/Session/)、
[EscapeAccess.Gameplay](../../InteractionFlow.Samples.EscapeGame/Aspects/Application/Entities/Access/EscapeAccess.Gameplay.cs)、
IGameplayAccess、4個のゲーム用SystemFlow/Checkpointと関連テスト。

GetSessionProgressはReadGameStateから6項目を詰め直すだけで、同期境界も追加しない。
呼び出し側は既に同じ契約でGameStateを取得できる。共有する不変状態が一つになった現在は、
この別DTOを維持する効果が小さい。

| 現在の参照 | 置き換え |
| --- | --- |
| GetSessionProgress(id).HasConfirmedAction | ReadGameState(id).Confirmed |
| GetSessionProgress(id).Outcome / RetryRequested | ReadGameState(id).Outcome / RetryRequested |
| GetSessionProgress(id).Stopping | ReadGameState(id).Mode == ExecutionMode.Stopping |
| Paused / Stage | DTO廃止時に削除。必要ならGameStateから読む |

- SessionProgressとGetSessionProgressの実装・interface宣言を削除する。
- 同一判断で複数項目を使うなら、一度取得した不変状態をローカル変数に置く。
- awaitやInteraction実行を跨いだ状態の再利用はしない。更新後の再取得位置を維持する。
- 停止済み・未公開IDでReadGameStateが返すStopping状態も維持する。
- 削減: record struct 1、メソッド実装1、契約宣言1。

DTOは値型なので、この削減によるヒープ割り当て削減は主張しない。目的は参照経路と契約数の削減である。

### C. Contextの同一検証をLifetimeContextへ移す【実施済み】

対象: EscapeApplicationContext、GameplayContext、ViewContextのValidateAdditionalValueと
[LifetimeContext](../../InteractionFlow.Samples.EscapeGame/Aspects/Lifetime/Entities/Contexts/LifetimeContext.cs)。

3個のoverrideはすべてContextAccessValidation.Validate(value!)への同一委譲である。
これをLifetimeContextのoverride一つへ移し、具象側のoverrideを削除する。
ContextAccessValidation自体は共有契約に置いたままでよい。

- 削減: メソッド実装2。クラスの追加なし。
- 役割別TryGetCore、親の探索遮断、Dispose、Cancellationの共有方法は維持する。
- 基底を直接使う派生Contextにも固定Access/Entry注入禁止が適用される点を明示する。
  現行3具象型の挙動は同じだが、公開基底の拡張時の契約は狭まるため、ContextTestsで確認する。

### D. 更新結果の検証をGameplayReaction内の2関数にまとめる【実施済み】

対象: 旧GameUpdateValidation。検証は[GameplayReaction](../../InteractionFlow.Samples.EscapeGame/Aspects/Gameplay/Externals/GameplayReaction.cs)へ移した。

実行時の利用者はGameplayReactionだけで、4関数のうちSelectionとRequiredTransitionの違いは
Rejectedを成功扱いするかどうかである。Unexpectedは例外生成だけである。

- GameplayReaction内にValidateUpdate(status, operation, allowRejected: false)を置く。
- Selectionの利用箇所だけallowRejected: trueを明示する。
- PresentationはValidatePresentation(change, operation)として残し、上の関数と提示キーの検証を使う。
- Unexpectedの例外生成をValidateUpdate内に直接記述する。
- GameUpdateValidationクラスを削除する。
- 削減: クラス1、メソッド実装2、ファイル1。

Applied / Rejected / Stoppedの許容差、未知のenum値の拒否、提示キー欠落の失敗を維持する。
この2関数は既存テストから直接確認できるinternalでもよい。
直接テストの参照先を移すだけで、失敗ケースの検証は減らさない。

### E. GameplayReactionの提示付き更新3本を共通化する【実施済み】

対象: [GameplayReaction](../../InteractionFlow.Samples.EscapeGame/Aspects/Gameplay/Externals/GameplayReaction.cs)の
ApplyPlayerActionAsync / ApplyEnemyActionAsync / PresentResultAsync。

3本とも、再開待機 → 更新 → Deferredなら再試行 → 結果・提示キー検証 → 提示と再開待機、
というほぼ同一の実装で、違うのはWorldRulesの関数と診断用の操作名だけである。

- 目的を限定したUpdateAndPresentAsync(context, update, operation)一つへ共通部分を移す。
- 既存の3メソッドは、対応するWorldRules関数と自身のnameofを渡す短い委譲にする。
- WaitForPresentationAndResumeAsyncはBeginSessionAsyncとも共有するため維持する。
- 通常の選択更新や制御更新まで含む汎用ディスパッチャへ広げない。
- 増減: メソッド実装1増。クラスは増えない。重複した非同期制御を一か所にする効果を優先する。

以下の違いは残す。

| 処理 | 共通化しない理由 |
| --- | --- |
| BeginSessionAsync | 未公開セッションを初期化・公開してから待つ。通常更新の再開待機を適用できない |
| HandleCommandAsync | Rejectedが無効操作の通常応答になる |
| BeginActionSelectionAsync / EnableResultInputAsync | 提示を新たに発生させない |
| ApplyControlRequest | ポーズ中も更新し、通常進行と独立した経路から呼ばれる |

再開確認後にもポーズ可能なので、ロック内更新のDeferred判定と再試行は必須である。
GameplayReactionは通常進行と制御で並行利用され得るため、updateやcontextを可変フィールドに保存しない。
既存のメソッドグループを使い、毎フレーム用の新規クロージャや独自スケジューラーを導入しない。

## 小さな整理と見送り候補（初回判断）

| 対象 | 判断 |
| --- | --- |
| RaylibWindowOperation / Reactionの空の明示コンストラクタ | 暗黙のpublic引数なしコンストラクタに任せられる。2定義削減。メソッド削減数とは別集計 |
| EscapeAccessのpartial 4ファイル | 同じクラスなのでファイル結合だけでは型・関数は減らない。同期管理の役割別閲覧に利点があり維持 |
| GameFlowCheckpoint | 再開・停止・終了時キャンセルの共通方針を持つ。各フローへ展開すると重複が増えるため維持 |
| 役割別ContextとAccess interface | アクセス制限とCancellationの寿命が異なる。全体Context一つに統合しない |
| GameInputOperation | 薄い委譲だが入力Port差し替えの境界。Accessを直接使う方式へ変更しない |
| PresentationClock / WindowState / FrameState | 演出時間、ネイティブ資源所有、フレーム受け渡しで寿命・利用者が異なるため維持 |
| GameLifetimeReaction | GameplayReactionへの統合で1クラス減らせるが、終了結果の集約と通常更新の関心が混在する。今回の推奨案に含めない |
| PresentationKeyのPresentationIdとVersion | 現在の生成では同じ値だが、古いキーの識別契約に関係する。型の定義数も減らないため見送る |
| WorldRulesとCommitのVersion更新 | 冗長に見えるが、規則単体の次状態生成と共有境界での版の正規化に役割がある。イベントキーとの整合を崩さないよう維持 |
| Viewの各Draw関数・座標ヘルパー | 画面の構成単位として読みやすさに寄与する。インライン化で巨大なDrawFrameにしない |
| PresentationClock.Updateの戻り値 | 本体は未使用でもFlowTestsが使用している。戻り型変更では定義数も減らないため維持 |

## 薄いInteractionを維持する理由（初回判断・下記の追補で見直し）

BeginSession / BeginActionSelection / ApplyPlayerAction / ApplyEnemyAction / HandleControlInput /
OpenWindow / RequestShutdownは、見た目としては一行委譲である。
ただしInteraction基底が、キャンセル登録、例外Port・CancellationPortへの委譲、
ReactionEndからInteractionEndへの変換を担当する。

SystemFlowからReactionを直接呼ぶとこれらの動作を失う。
一つの汎用Interactionにまとめても、段階識別enum・設定・インスタンス生成・依存列挙が新たに必要になり、
既存の型ベースDIでは段階別インスタンスの構成も変わる。
別のInteractionをInteractionから呼ぶ方式や、プレイヤー・敵の進行合成をReactionへ移す方式も採用しない。

現在の4段階のゲーム用FlowとWindowSystemFlowは、ターン、セッション、リトライ、画面寿命を表している。
GameTurnSystemFlowをセッションへ展開して型を1個減らす案も、ターン単位の読みやすさを失うため推奨しない。

## 実施範囲と削減実績

| 内容 | 型の純減 | メソッド実装の純減 |
| --- | ---: | ---: |
| A: Profileは維持 | 0 | 0 |
| B・C: 派生DTOと同一overrideの削除 | 1 | 3 |
| D・E: 検証の集約と提示付き更新の共通化 | 1 | 1 |
| 合計 | 2 | 4 |

D・Eの内訳は、検証4→2で2減、共通更新関数追加で1増である。
interfaceのメソッド宣言1個と、空コンストラクタ2個の削減は上の4個に含めない。
型数は78→76、クラスは46→45、record類は9→8、C#ファイルは66→65、行数は2,407→2,350となった。
新しい汎用基底・戦略interface・ヘルパー型は追加していない。

## 動作を維持するための確認

各段階でEscapeGame.Testsを実行する。新規テストは不足する契約がある場合だけ追加し、
既存109ケースの意味を維持する。段階2ではDTO参照先の置換によってStopping判定などが反転しないことを確認する。

| 確認する契約 | 主な既存テスト |
| --- | --- |
| 12ターン解法、勝利後に敵を動かさない、敗北・無効操作 | DomainTests、FlowTests |
| 入力一件配送、処理中入力を蓄積しない、古いセッションの拒否 | SharedGameStateTests、AccessTests、FrameTests |
| 不変状態、並行更新、例外発生時に次状態を公開しない | SharedGameStateTests |
| ポーズ直後の更新を延期し、再開後に一回だけ適用 | GameplayReactionTests、AccessTests、FlowTests |
| 提示完了後に進行・結果入力を開始、古い提示キーを拒否 | GameplayReactionTests、ResultInteractionTests、AccessTests |
| Contextの探索制限、Dispose、値/Cancellationの継承 | ContextTests、LifetimeAccessTests |
| 停止・キャンセル・先行失敗・終了処理失敗の保持と合流 | CleanupTests、FailureTests、FlowTests |
| 文言・ボタン表示、ポーズ境界時間の破棄、演出省略 | WindowReactionStateTests |

実装完了時にはAGENTS.mdに従い、昇格付きで次を確認する。

```powershell
dotnet build InteractionFlow.slnx
dotnet test InteractionFlow.slnx
dotnet build benchmarks/InteractionFlow.ExecutionMeasurements/InteractionFlow.ExecutionMeasurements.csproj
```

実画面で、移動の仮選択・取消・確定、演出中のP/F、勝敗、Rによるリトライ、Exit・ウィンドウ閉鎖を確認する。
ゲーム待機中も描画が継続し、再開・終了で待機が残らないことを確認する。
性能向上は今回の主目標にせず、現行計測と同条件でPresentFrame等の割り当てが悪化していないことを確かめる。
現在の計測値は実描画を含まないため、全体の高速化率は算出しない。

## 実装後の検証結果

- 全ソリューションビルド: 警告0・エラー0。
- 全ソリューションテスト: EscapeGame 109件と他470件、計579件成功。
- Contextの既存テストを拡張し、LifetimeContextだけを使うルート・子Contextにも
  Access/Entry注入禁止が継承され、通常の追加値は利用できることを確認した。
- Release計測プロジェクトはビルド・実行成功。描画なしフレームの割り当ては従来値を維持した。
- 実画面で移動の仮選択・取消・確定、ターン進行、ポーズ中の入力抑止、Pでの再開、
  敗北結果の提示、Rでの初期状態へのリトライ、Exitでの閉鎖を確認した。
- 勝利解法、演出省略、演出中のポーズ競合、終了時の合流は自動テストで確認した。
  今回の実画面ではこれらのタイミング操作とOSの閉じるボタンは再確認していない。

.NET 10.0.14 / x64、TieredCompilation=0、20,000回ウォームアップ、200,000回×3。

| 計測対象 | 割り当て | 今回の時間範囲 |
| --- | ---: | ---: |
| 不変GameStateの取得 | 0B | 14.3〜17.0ns |
| 直接フレーム（取得2回・ローカル時計） | 0B | 107.2〜112.3ns |
| PresentFrame一回 | 232B | 204.5〜214.4ns |

実画面のゲームも起動中の計測であり、従来記録と同時条件の対照測定は行っていない。
時間差から高速化・低速化の結論は出さない。実描画・表示文言生成・ゲーム全体の負荷を含む結果でもない。
サンプルREADME、配置表、共有状態の契約文書も実装に合わせて更新した。

関連: [現在の配置](./ESCAPE_GAME_NAMESPACE_LAYOUT.md)、
[共有状態と同期契約](./ESCAPE_GAME_GAMEPLAY_RULES.md)、
[利用設計指針](../guides/INTERACTIONFLOW_APPLICATION_DESIGN.md)。

## 追補: ターンと結果確認の単位への集約

初回の「薄いInteractionを維持する」という判断を見直した。
Interaction基底の例外・キャンセル処理は必要だが、その境界を一つのPort呼び出しごとに設ける必要はない。

- GameTurnSystemFlowとBeginActionSelection / HandleGameInput / ApplyPlayerAction / ApplyEnemyActionをPlayTurnへ集約。
- PresentResultと結果段階でのHandleGameInputによる反復をHandleResultへ集約。
- GameplayのInteractionは8個から5個となり、GameTurnSystemFlowも削除。合計4クラス減。
- GameSessionSystemFlowはPlayTurnの反復とHandleResultへの遷移を担う。
- GameFlowCheckpointはGameplay.InteractionsのGameplayCheckpointへ移し、ReactionEndを受け取る。
  SystemFlow側はInteractionEnd.Endを渡し、Interaction内の段階境界と同じ継続方針を使う。
- 状態更新・提示待機は既存Reaction、並行する制御経路はHandleControlInput、実行寿命と合流はSystemFlowに維持。

削除した小さなInteractionを内部実行する構成にはしていない。
エラーの診断名とキャンセル登録の単位はPlayTurn / HandleResultとなる。
この粒度変更を踏まえ、段階境界の失敗・キャンセルで後続を実行しないことと、
結果提示前に入力せず、無効入力後もリトライ待機を継続し、キャンセル時に登録と入力待機を解除することを検証する。

集約後の全体ビルドは警告・エラー0件。追加8ケースを含むEscapeGame123件と他470件、全593テストが成功した。
実画面でターン進行、ポーズ・再開、敗北結果、リトライ、入力待ちからのExit終了を確認した。
勝利解法や演出中のキャンセル・省略は自動テストで確認し、今回の実画面では再実施していない。
計測プロジェクトのReleaseビルド・実行も成功し、描画なし直接フレーム0B / PresentFrame232Bを維持した。
この計測は入力待ち相当のフレームであり、PlayTurn / HandleResult自体の割り当てや時間は測定していない。
