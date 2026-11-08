# Raylib counter

raylib-cs のフレーム処理と非同期の相互作用を、SystemFlow で合成するサンプルです。

```powershell
dotnet run --project InteractionFlow.Samples.Raylib
```

デスクトップの描画環境が必要です。Raylib-cs 8.1.0 のネイティブライブラリを NuGet から取得します。
画面の文字列は標準フォントで表示できる英語にしています。

- **Run +1**: 1 秒待ってからカウントを増やします。処理中の連打は蓄積しません。
- **Cancel**: 入力待ち・処理を中断し、キャンセルの待機・リセット後に再入力を受け付けます。
- **Exit**: 入力受付中に終了操作を渡し、Reaction の完了後に SystemFlow が終了を判断します。
- **Esc / ウィンドウを閉じる**: 終了意図を保持し、カウンターをキャンセルして終了を待ちます。

## フローの構成

`Program` は Profile の適用、Scope / Context の構築、`WindowSystemFlow` の実行だけを担当します。

```text
Program
  └─ WindowSystemFlow（メインスレッド）
       ├─ OpenWindow → WindowReaction.Open / Draw
       ├─ CounterSystemFlow（Task.Run、フレームと並行）
       │    └─ IncrementCounter → CounterOperation.ReadAsync → CounterReaction
       ├─ PresentFrame（繰り返し）→ WindowOperation.Poll → CounterOperation.Submit → WindowReaction.Draw
       ├─ カウンターへのキャンセル通知・終了待機
       └─ CloseWindow → WindowReaction.Close（終了・通知・全体結果の確定）
```

WindowSystemFlow がフレームの継続、終了要求、処理失敗、終了待機を調整します。
WindowOperation は `FrameState` に Command / CloseRequested / CancelRequested を毎フレーム記録します。
無入力なら Command は null に戻ります。`PresentFrame` は終了・キャンセル要求がないときだけ、
記録されたコマンドを CounterOperation へ配送し、描画 Reaction を実行します。
配送時に入力待機がなければ破棄し、後続フレームや次の入力待ちへ蓄積しません。
Interaction が直接別の Interaction を実行する構成はありません。
Open・各フレーム・Close・全体結果の通知も、明示 Reaction の終了結果で完了します。

## Function の責務

| 分類 | Port / 実装 | 責務 |
| --- | --- | --- |
| Operation | `IWindowOperation` / `RaylibWindowOperation` | 入力・終了要求を FrameState に記録 |
| Operation | `ICounterOperation` / `CounterOperation` | 一つのコマンドを非同期に待機・受理 |
| Reaction | `ICounterReaction` / `CounterReaction` | 処理中・完了の表示更新、カウント・終了意図の更新 |
| Reaction | `IWindowReaction` / `RaylibWindowReaction` | ウィンドウの提示・描画・終了、結果の通知と確定 |

カウント・表示・入力受付・終了意図は `Entities/CounterState.cs` に集約します。配置定数の `RaylibLayout` は
raylib 実装内部の補助で、入力判定と描画が同じ座標を利用するために共有します。
ウィンドウの生成・解放は、画面の提示・消失という Reaction の一部として `RaylibWindowReaction` が直接行います。
`Entities/WindowState` はフレーム用 Context に置く開閉状態・所有スレッドです。
入力・描画・Close は同じ文脈値を取得します。DI では状態オブジェクトを登録しません。
ネイティブ API やリソース管理を別 Function に切り出さず、SilentExternal の分類は使っていません。
各 Function は依存する Port をコンストラクターで受け取り、Dependency にも列挙します。
Context に画面実装や接続オブジェクトを格納しません。カウンターとフレームの Context へ同じ
`CounterState` を直接登録します。独立した記録・キャッシュはないため Storage は使いません。

## カウンターの状態遷移

| 操作 | 遷移・役割 |
| --- | --- |
| `ReadCommandAsync` | Ready → WaitingForInput。終了要求済みなら新規待機を作らず Exit を返す |
| `TrySubmit` | 待機が有効な場合だけ受理し、WaitingForInput → Processing。連打や待機外の入力は破棄 |
| `BeginIncrement` | 受理済みの処理開始を確認し、処理中の表示へ更新 |
| `CompleteIncrement` | カウント更新と Processing → Ready を同時に行う。終了要求後は更新しない |
| `Cancel` | 入力待機の後始末後に Ready へ回復。終了・失敗状態を上書きしない |
| `Fail` | Failed へ遷移し、失敗を表示 |
| `RequestExit` | 終了意図を保持して受付を閉じる。既存の失敗表示は保持 |

キャンセルされた入力待機は、同じ待機元であることを確認して通知参照を解除します。
待機中の終了要求だけでは Task を完了させず、SystemFlow のキャンセル通知で待機を解除します。
終了・中断・処理完了が競合しても、終了意図を入力受付へ戻す遷移はありません。
Count と ExitRequested は取得専用で、外部から個別に書き換えません。
## スレッド・Context・終了契約

- `WindowSystemFlow` は同期的にフレームを進め、Open から Close まで呼び出しスレッドを維持します。
  `IWindowOperation` / `IWindowReaction` は同期完了する契約です。フレーム内で非同期描画を待ちません。
- カウンターだけを `Task.Run` で起動します。カウンターの Operation / Reaction は raylib を呼びません。
  フレームの描画完了も待たないため、終了時にメインスレッドがカウンターを同期的に待てます。
- フレーム用 Context は独立したルートです。親ありの `FlowContext` はキャンセルを共有するため、
  この並行処理の分離には使いません。終了用の子 Context はフレームが終わった後に作ります。
- `CounterState` は両フローで共有し、カウント・表示・進行段階・入力待機を一つの private ロックで管理します。
  入力受付表示と実際の受理条件は同じ判定から導出し、独立した bool の更新は行いません。
- `Snapshot` は count / message / acceptingInput を同じロック内で out 引数へ取得します。
  描画側は取得済みの値を使い、その後の更新で描画中の値が変わることはありません。
- 入力の非同期待機は State 内の TCS で維持します。ロック内では await せず、継続は非同期実行します。
  Operation は Context から State を取得して待機・操作要求を委譲するだけで、待機状態を保持しません。
- Context / Scope 一般の並行安全性を仮定していません。同じ Function が並行利用される部分では、
  入力・表示の共有状態を明示的に同期し、実行中に登録の変更や状態リセットを行いません。
  共有する表示状態は両フロー開始前に登録します。状態を差し替えないため RefEntry は使いません。
  WindowState はメインスレッドでのみ扱い、カウンター用 Context には渡しません。
- OS の終了要求は Operation が FrameState に記録します。SystemFlow はこの要求でループを抜け、異常終了時も含め、
  最後に終了意図を保持し、キャンセル・カウンターの終了待機・Close を順に行います。
- SystemFlow は Open / フレーム / カウンターの結果を集約し、終了用の子 Context に `Result` として追加します。
  `CloseWindow` はこの値を `IWindowReaction.Close(context, result)` に渡します。Close の Reaction が
  画面の終了・コンソール通知を行い、それらの失敗も先行結果へ合成して全体の終了結果を返します。
  Close の送出例外も処理 Port で先行結果を保持します。起動に失敗した場合も Close を試みます。
- CounterReaction の完了は表示データの更新までです。描画 Reaction は `EndDrawing` までを完了とします。
  個々の表示更新が必ず一度描画される契約ではなく、終了直前の Goodbye 等の表示保証はありません。
  実際の画面提示や演出の完了を待つ仕様は、今後の別の検証対象です。

## 検証

`InteractionFlow.Samples.Raylib.Tests` はネイティブ画面を使わず、Port を差し替えて検証します。
入力の重複抑制、キャンセル・再開、フレームとカウンターのキャンセル分離、
描画スレッドの維持、終了待機、起動・処理・終了の失敗保持を対象とします。

手動では、黄色い点が入力待ち・処理中とも動くこと、Run の連打、Cancel 後の再入力、
入力待ち・処理中のウィンドウ終了、Exit による終了を確認できます。
