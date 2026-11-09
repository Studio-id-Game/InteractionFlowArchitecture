# EscapeGameの利用側改善プランと実装結果

状態: 段階1〜5とInteraction粒度の見直しをサンプル内へ実装済み。
全体ビルド（警告・エラー0件）、全体テスト527件、Release簡易計測を確認済み。今回の実画面での再確認は未実施。
対象はEscapeGame、そのテスト・計測・関連文書。今回の改修ではCore / Standard / Analyzerのコードと公開契約を変更していない。

## 判断基準

必要な責務分離、薄いInteraction、明示登録、ポーズ時の二重確認を維持した。
意味が明確になり、変更時の調査とテストが容易になる修正を採用した。
新しい汎用フロー基底、DSL、スケジューラーや、テストのためだけのContext差し替え機構は追加していない。

## 段階1：終了処理の暗黙契約を明示

`Finishing(FinishingKind, Result)`をサンプル内に追加した。
`CloseWindow`と`EndSession`は処理種別を確認してから、先行結果をReactionへ渡す。
両Handlerもこの明示情報を使い、通常停止の回復と終了処理自体のキャンセル失敗を区別する。
通常Contextへ無関係なResultを追加しても、キャンセルの意味は変わらない。

Finishingを登録するのは、Close / EndSession専用の子・独立Contextだけである。
通常セッションやフレームのContextへ漏らさず、毎フレームの値や割り当ては増やしていない。
送出・キャンセル・リセット失敗と先行失敗の組合せはFinishingTestsで検証する。

## 段階2：内部の不成立と通常の無効操作を区別

GameReactionの合法な更新結果を操作別に分類し、次の処理にした。

| 更新結果 | 選択 | 必須の段階更新・行動・提示 |
| --- | --- | --- |
| Applied | 成功 | 成功。提示が必要ならPresentationKeyを必須とする |
| Rejected | 通常の無効操作としてフィードバック | フローの前提違反として失敗ReactionEndを返す |
| Deferred | 同じ仕事を保持して再開待機後に再検証 | 同左 |
| Stopped | 通常の終了 | 同左 |

Beginは未公開セッションを公開する明示的な経路に分離した。
Preparingはポーズ対象外のため、公開前にActiveの再開待機を要求しない。
通常更新との切替に使っていたwaitBeforeApplyと、capturing Funcによる更新の転送を削除した。

二重Begin、確定なしのApplyPlayer、提示未完のApplyEnemy、勝敗未確定のShowResult、
EnableSelection / EnableResultInputの前提違反を、実際のGameReactionで検証する。
Appliedなのに提示keyがない場合や未知のstatusは、小さなinternal検証関数のテストで確認する。
ContextやAccessの差し替え口は増やしていない。通常経路の検証は例外を生成しない。

## 段階3：進行確認と失敗保持を共有

Turn / SessionSystemFlowの共通方針を`GameFlowCheckpoint`へ抽出した。
失敗・停止を確認し、ポーズ中は再開を待ち、待機後も停止を再確認する。
Access側のActive再確認と一括更新は残しており、フロー側の確認だけで原子性を保証しない。

`FailureCollector`は、終了時に観測した失敗を順に保持するサンプル内補助とした。
成功時は失敗リストを作らず、一件の原因の同一性、複数原因の順序、既存の集約・ラッパーを保つ。
通常Result連結とは区別し、例外捕捉の範囲を広げるための共通化にはしていない。
汎用フロー基底や共有の可変な「現在の処理種別」は導入していない。

## 段階4：SessionとWorldの責務を分離

`IGameSessionAccess` / `Context.Session`へ、ActiveSessionId、ShutdownRequested、GetProgress、
BeginSession、EndSession、RequestShutdownを移した。
`IGameWorldAccess` / `Context.World`は、選択・プレイヤー行動・敵行動・結果表示・選択受付・結果受付の更新に絞った。
実装は同じEscapeAccessと一つのgateを共有し、管理オブジェクトやロックを追加していない。
Contextがすべての契約を提供するため、Portが最小権限になったという意味ではない。

`ApplicationSessionId = 0`は、特定のゲームセッションを指さないアプリ・フレーム用の固定対象を表す。
アクティブセッションに自動追従するIDではなく、実セッションIDは1から割り当てる。

| Context生成 | 共有 | 分離・用途 |
| --- | --- | --- |
| 型付き親ありコンストラクター | アクセス、対象ID、Cancellation、親の値探索 | 子自身の追加値 |
| CreateIndependentContext | アクセス、固定された対象ID | Cancellation、追加値。フレーム分離・停止用に使用 |
| CreateSessionContext | アクセス、ゲーム実行側のCancellation、親の値探索 | 新規IDと初期状態。公開はBeginSessionのReactionが行う |

生の可変Stateを外へ返さない構成、追加値・Entryによる固定契約の隠蔽拒否、破棄後の取得拒否は維持した。
この段階では強いID型やContext型の追加分割を見送り、既存の提示キーによる識別を維持した。

### 後続改善：SystemFlowの範囲に応じたContext分離

WindowSystemFlowにはWindowContext、Game / Session / TurnSystemFlowにはEscapeApplicationContextを渡す。
どちらも同じEscapeAccessを共有するが、Window側はWorld、ゲーム側はDisplayを提供しない。
入力契約もIGameInputWriterとIGameInputReaderに分け、各ContextのプロパティとTryGetCoreで同じ範囲を提供する。
Session / Presentation / Controlは共通の契約を維持し、メソッド単位の最小権限化までは行わない。

WindowからゲームContextを作る際は、アクセス以外の追加値・Cancellation・親探索を引き継がない。
WindowSystemFlowは外部キャンセルをフレーム境界等で観測して既存の停止・合流へ接続する。
同種Context内の子スコープは従来どおり親探索とCancellation共有を行う。
固定アクセスとEntryの追加拒否により、標準の追加値経路から提供範囲を変えない。
ただし返却したAccess実体へのキャストを防ぐ機構ではなく、悪意あるコードに対する隔離は目的としない。

分離後の全体ビルドは警告・エラー0件、テストはEscapeGame92件と他プロジェクト445件が成功した。
非公開契約の探索・追加拒否、親探索とCancellation共有の境界、破棄の独立性、入力通信を検証した。
外部キャンセル時は停止の成功・失敗いずれでも、実ゲーム側のTask登録解放と合流後のCloseを確認している。
Release簡易計測で通常フレームの488B/回を維持した。実画面での再確認は未実施。

## Interaction粒度の見直し

`ShowResult`へ「結果提示を待つ→失敗なら短絡→結果入力を開く」を統合し、
独立したEnableResultInput Interactionとその登録を削除した。Functionのメソッド自体は維持している。
この間にはSystemFlowによる別の選択がなく、結果段階はポーズ対象外で、一つの完了範囲として扱える。
停止との競合は受付更新側で再確認し、先行Reactionの失敗時は受付へ進まない。

`EnableSelection`と`SelectAction`は維持した。
前者は初期・各ターンの選択段階を開く境界、後者は仮選択・取消・確定・無効入力を一件ずつ受けて反復する境界である。
両者をまとめて入力反復をInteractionへ隠すと、SystemFlowの選択・反復と再開確認が見えにくくなる。
SelectActionは結果段階でのリトライ入力にも使われる。
プレイヤー・敵行動も、提示待機と途中の勝敗判断が異なるため独立した境界を保った。
ポーズ等の制御はゲーム入力・提示待機と独立したままにしている。

## 段階5：ナビゲーションと検証を整理

サンプルREADMEへ、呼び出し経路、Contextの生成・所有、完了範囲と以上の判断を追加した。
テストの提示完了・初期化は`GameTestSteps`へまとめ、更新規則そのものはテスト補助へ複製していない。
ForceResetMemoryState、明示Dependency、薄いInteractionのうち意味のある境界は維持した。

## 受け入れ条件と検証

- 12ターン脱出と両捕獲、リトライ、結果提示後の受付を維持する。
- 受理済み入力・確定要求の保持、ポーズ中の更新・時間・ack抑止、再開後の一回適用を維持する。
- 古いセッション・提示の通知と省略を拒否し、入力を蓄積しない。
- 起動前停止、提示・入力・再開待機中の停止、Stop失敗時のcancel / join / closeを維持する。
- Close / EndSessionの送出・キャンセルでも先行失敗を失わない。
- 変更に対応する回帰テストと影響範囲・全体テストを実施する。
- Release簡易計測で、フレーム実行とスナップショットの割り当て増加を確認する。
- 入力・描画経路を変更した場合は実画面で操作・ポーズ・再開・リトライ・終了を確認する。

各段階はレビュー・指摘修正・再レビューを経てコミットする。
具体的な実行・計測結果は[サンプルREADME](../../InteractionFlow.Samples.EscapeGame/README.md)と
[計測プロジェクト](../../benchmarks/InteractionFlow.ExecutionMeasurements/README.md)を参照する。

## 対象外

ライブラリ原則や公開API、依存宣言の自動化、型による完全な権限制御、全状態の自動同期、
汎用的な全体ポーズ、描画環境への無変更移植、Interactionの実行固定費削減は変更していない。
それぞれ意図した責務や別のトレードオフであり、この利用側改修の未達欠陥とは扱わない。
