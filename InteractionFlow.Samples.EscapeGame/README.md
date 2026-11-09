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

Programは構成と起動を担当し、WindowSystemFlowがメインスレッドのフレームと背景のゲーム進行を所有します。
GameSystemFlowはセッションを繰り返し、SessionSystemFlowとTurnSystemFlowが状態に応じてInteractionを合成します。
ゲーム側は演出提示の完了を非同期に待ちますが、フレーム側はその間も描画・制御入力を継続します。

Entitiesは描画環境から独立した状態と安全なアクセス管理を提供します。
専用Contextは用途別アクセスを提供し、ゲームの進行やTaskの起動は担当しません。
ポーズは状態更新の同期境界とフローの再開待機で協調的に行い、キャンセルやセッション再作成で代用しません。
アプリ終了はポーズより優先し、提示待ち・入力待ち・再開待ちを解除してからゲーム終了を待ち、Closeへ進みます。

入力・描画はネイティブPort、ゲームの状態更新・応答はゲームPortに分けます。
画面依存の配置・色・描画の補助はExternals内部に置きます。
表示スナップショットと提示IDを使い、状態更新・画面提示・演出完了を区別します。

提示を最初に取得したフレームの経過時間は0とします。
ポーズ・再開の制御版が前フレームから変わった場合、その境界をまたぐdeltaは保守的に破棄します。
ポーズ中の実時間を加算しない代わりに、最大一フレーム分の動作時間を捨てる場合があります。
提示完了・演出省略はセッションIDと提示ID・版を検証し、古いフレームの要求を次の提示へ適用しません。

## 一操作をコードから追う

1. [RaylibWindowOperation](./Externals/RaylibWindowOperation.cs) が押下をFrameStateへ記録し、
   [PresentFrame](./Interactions/PresentFrame.cs) が優先順位を確認してGameOperationへ配送します。
2. [SelectAction](./Interactions/SelectAction.cs) が入力Operationを一件待ち、
   [GameReaction.SelectAsync](./Externals/GameReaction.cs) がWorldアクセスへ選択・取消・確定を要求します。
3. [TurnSystemFlow](./SystemFlows/TurnSystemFlow.cs) がSessionの進行参照から確定を確認し、
   ApplyPlayerActionを選びます。Reactionは論理状態を更新して提示通知を待ちます。
4. [RaylibWindowReaction](./Externals/RaylibWindowReaction.cs) が表示を取得し、提示を描画した後に完了を報告します。
   フローは再開・停止を確認し、勝敗が未確定なら敵行動へ進みます。

ゲーム上の無効な選択は、理由を表示する正常なRejectedです。
確定なしの行動、未完了提示を飛ばす段階更新などの内部前提違反は、失敗ReactionEndとして返します。
Deferredは同じ仕事を保持して再開後に再検証し、Stoppedは終了として扱います。
初期化は未公開セッションを公開する明示的な経路で、公開前の再開待機は要求しません。

## Contextとアクセスの所有

[WindowContext](./Entities/Contexts/WindowContext.cs) はWindowSystemFlowと画面側Interactionへ、
[EscapeApplicationContext](./Entities/Contexts/EscapeApplicationContext.cs) はGame / Session / TurnSystemFlowと
ゲーム側Interactionへ渡します。関心別Stateの協調更新は、両Contextが共有する一つのEscapeAccessとgateで行います。

| 提供する値 | WindowContext | EscapeApplicationContext |
| --- | --- | --- |
| 入力 | IGameInputWriter（送信） | IGameInputReader（待機） |
| Session | あり | あり |
| World | なし | あり |
| Display | あり | なし |
| Presentation / Control | あり | あり |

プロパティだけでなくTryGetCoreも同じ提供範囲に限定します。例えばWindowContextから
IGameWorldAccess、ゲームContextからIGameDisplayAccessやIGameInputWriterは探索できません。
実体の全インターフェースを公開する型判定は使いません。Session / Presentation / Controlは
現時点では各契約全体を共有しており、メソッド単位の権限制限にはしていません。
返したAccess実体へのキャストまで防ぐセキュリティ境界ではありません。

| アクセス | 用途 |
| --- | --- |
| Session | 公開・終了・アプリ停止、進行状態の参照 |
| World | 選択、プレイヤー・敵行動、結果表示、選択・結果の受付開始 |
| Input | 一件の入力待機とセッションID付きの受理 |
| Control | ポーズ・再開と協調的な再開待機 |
| Display / Presentation | 一貫した表示の取得、識別付き提示要求の進行・完了 |

Session / Worldの分離は、同じアクセス管理の呼び分けです。
管理オブジェクトやロックを増やしていません。
今回の利用側改修ではCore / Standard / Analyzerのコードと公開契約を変更していません。

`ApplicationSessionId = 0`は、特定のゲームセッションを指さないゲーム実行ルートの固定対象です。
WindowContext.CreateGameContextはアクセスだけを共有する独立したゲームContextを作り、
Window側の追加値・Cancellation・親探索を引き継ぎません。ゲーム実行側はこの独立Contextを持ち、
そこからCreateSessionContextで新規ID・初期状態を割り当てます。
そのセッションはゲーム側Cancellationを共有し、BeginSessionのReactionで公開されます。
CreateIndependentContextはアクセスと対象IDだけを共有し、Cancellationと追加値を分離します。
型付きの親ありContextは、アクセス・対象ID・Cancellationを共有し、追加値を親へ探索します。
各実行の所有者がTaskの終了を待ち、ContextのDisposeで停止や合流を代用しません。
WindowSystemFlowは呼び出し元のCancellationを起動前・ゲーム開始前・各フレームの境界で観測し、
停止・ゲーム側Cancellation・合流・Closeの経路へ接続します。Window側のキャンセルをゲーム側へ
直接共有しないため、Window側で要求されたキャンセル状態はゲーム側のリセットでは解除されません。
開始前にキャンセル済みならOpenとゲーム開始を省略し、停止・Closeを実行します。

## 完了範囲と終了結果

[ShowResult](./Interactions/ShowResult.cs) は「結果提示の完了を待つ→結果入力を開く」を一つの相互作用として扱います。
提示Reactionの失敗時は短絡し、受付開始へ進みません。間にフロー選択はなく、結果段階はポーズ対象外です。
停止との競合は受付更新側で再確認します。独立したEnableResultInput Interactionは削除しました。

EnableSelectionとSelectActionは維持しています。選択段階を開く境界と、一件の入力へ応答して反復する境界は別で、
その間と各反復にはフローの再開・停止確認があります。SelectActionは結果段階のリトライ入力にも使います。
プレイヤー・敵行動の間にも勝敗の選択があるため、薄いという理由だけで相互作用を一律にまとめていません。

[GameFlowCheckpoint](./SystemFlows/GameFlowCheckpoint.cs) はフローの失敗・停止・再開確認を共有します。
更新側でもポーズを再確認して一括反映するため、チェックポイントだけで原子性を保証する設計ではありません。
[FailureCollector](./Entities/FailureCollector.cs) は終了時の原因を観測順に保持し、通常のResult連結とは区別します。

Close / EndSessionの専用スコープへは、[Finishing](./Entities/Finishing.cs)で処理種別と先行Resultを渡します。
通常Contextに無関係なResultがあっても、終了処理中という意味にはなりません。
終了の送出例外やキャンセルも先行失敗へ合成し、通常停止の回復と終了処理自身の失敗を分けます。
通常フレームや通常セッションへFinishingを持ち越さず、終了処理のためだけに渡します。

状態更新、描画提出、演出完了は異なる範囲です。ゲーム側Reactionは必要な提示の通知まで待ちますが、
ユーザーが実際に認識したことまでは保証しません。全体の合流・停止・資源解放はWindowSystemFlowが調整します。
改修の判断と対象外は [利用側改善プランと実装結果](../docs/design-notes/ESCAPE_GAME_APPLICATION_REFACTOR_PLAN.md) を参照してください。

## 検証

`InteractionFlow.Samples.EscapeGame.Tests`は画面を開かず、Portや提示通知を差し替えて検証します。
操作と確定の分離、鍵と解錠の整合性、勝敗、ポーズ境界、古い通知、リトライ、失敗・終了待機を確認します。
実画面では描画継続、補間、入力、ポーズ・再開、結果表示と終了を確認します。
GameReactionTestsは通常の無効選択と内部前提違反、FinishingTestsは明示終了情報、
FailureTestsは原因の同一性・集約順序、ResultInteractionTestsは提示から受付への短絡・停止を検証します。
共通の初期化・提示完了手順はGameTestStepsに置き、ゲーム規則そのものは複製しません。

Context分離後は、EscapeGameの92件と他プロジェクトの445件が成功しています。
全体ビルドは警告・エラー0件です。ContextTestsでは公開・非公開契約の探索、固定契約の追加拒否、
親探索とCancellationの境界、破棄の独立性を確認します。FlowTestsでは外部キャンセルと停止失敗時にも、
ゲーム側のTask登録が解放され、セッション終了・合流後にCloseへ進むことを確認します。
Release簡易計測はSnapshotが128B・約33～34ns、直接フレームが256B・約139～143ns、
PresentFrameが488B・約232～242nsでした。測定条件は下記と同じで、割り当て量は維持しています。
Context分離後の実画面確認は未実施です。

## 利用側改修後の確認結果

全体ビルドは警告・エラー0件、全体テストは527件成功しました。
同じ条件のRelease簡易計測では、フレームの割り当て量は初版から増えていません。

| 対象 | 割り当て | 時間の目安 |
| --- | ---: | ---: |
| 表示スナップショット一件 | 128B | 約34～37ns |
| 入力・表示の本体を直接実行（スナップショット二件） | 256B | 約140～151ns |
| PresentFrameを一回実行 | 488B | 約239～247ns |

時間は簡易計測時の観測値であり、初版と同じ速度やゲーム全体の性能を保証するものではありません。
今回の利用側改修後には、実画面での再確認は行っていません。以下の実画面記録は初版時点のものです。

## 参考：初版の確認結果

自動テストでは12ターンの解法による脱出とリトライ、状態の整合性、ポーズ・再開、
提示の保留、古い通知、正常終了の競合、終了・通知の失敗保持を確認しています。
実画面では移動の選択・確定、敵移動、入力待ちからのポーズ、停止中の入力破棄、再開、
敗北表示、リトライ、再開せずに終了できることを確認しました。

描画をfakeへ差し替えたReleaseの簡易計測（.NET 10.0.14/x64、段階的JIT無効、
20,000回ウォームアップ後200,000回×3）では、入力待ち相当の一フレームは以下でした。

| 対象 | 割り当て | 時間の目安 |
| --- | ---: | ---: |
| 表示スナップショット一件 | 128B | 約36ns |
| 入力・表示の本体を直接実行（スナップショット二件） | 256B | 約131～136ns |
| PresentFrameを一回実行 | 488B | 約215～218ns |

通常フレームはPresentFrame一回で、制御操作がある場合はChangeControlも実行します。
演出中は提示スナップショットの更新、ターン進行ではゲーム側のInteraction・待機通知等が追加されます。
ネイティブ入力・文字列整形・描画・GPU待ち・スレッド競合・実フレーム時間はこの計測に含めていません。
ゲーム全体や演出中の性能保証として扱いません。
再実行は [計測プロジェクト](../benchmarks/InteractionFlow.ExecutionMeasurements/README.md) を参照してください。
