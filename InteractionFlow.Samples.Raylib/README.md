# Raylib counter

raylib-cs のフレーム処理と非同期の相互作用を、SystemFlow で合成するサンプルです。

三文書との対応、終了時の文脈更新・画面提示に関する留保は
[アーキテクチャとの整合性分析](./ARCHITECTURE_ANALYSIS.md) にまとめています。

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

カウンターの遷移規則は `Entities/CounterState.cs`、表示内容は `CounterDisplayState.cs` に分けます。配置定数の `RaylibLayout` は
raylib 実装内部の補助で、入力判定と描画が同じ座標を利用するために共有します。
ウィンドウの生成・解放は、画面の提示・消失という Reaction の一部として `RaylibWindowReaction` が直接行います。
`Entities/WindowState` はフレーム用 Context に置く開閉状態・所有スレッドです。
入力・描画・Close は同じ文脈値を取得します。DI では状態オブジェクトを登録しません。
ネイティブ API やリソース管理を別 Function に切り出さず、SilentExternal の分類は使っていません。
各 Function は依存する Port をコンストラクターで受け取り、Dependency にも列挙します。
カウンターについては、Context は生の可変 State を提供せず、安全なアクセス契約を提供します。
独立した記録・キャッシュはないため Storage は使いません。

## Context と状態へのアクセス

`CounterFlowContext` は `FlowContext.TryGetCore` で次の値を提供します。
Context 自身はゲームの進行、Task の実行、待機を行いません。

| Context から取得する値 | 利用箇所と役割 |
| --- | --- |
| `ICounterInputAccess` | Operation が入力待機と受理を要求する |
| `ICounterStateAccess` | Reaction がゲーム状態の遷移を要求し、SystemFlow が終了意図を読む |
| `ICounterDisplayAccess` | WindowReaction が表示する値をまとめて取得する |

```csharp
var input = context.GetRequired<ICounterInputAccess>();
var state = context.GetRequired<ICounterStateAccess>();
var display = context.GetRequired<ICounterDisplayAccess>();
```

内部の `CounterAccess` は一つの gate を持ち、`CounterState`（ゲームの規則）、
`CounterDisplayState`（表示内容）、`CounterInputWait`（入力通知の仕組み）を調整します。
例えば操作受理では受付条件の確認・段階の変更・通知を、完了ではカウントと表示の更新を、
同じロック内で行います。State 自体は同期や Task を管理しません。

単に GetRequired の実行中だけをロックする設計ではありません。
提供するアクセス契約の各操作が、必要な複数 State へのアクセス範囲を保護します。
カウンターのアクセス契約は生の可変 State を返さず、表示は out 引数、進行状態は値として返します。
非同期処理はロック外で待ち、待機後に再び同期範囲へ入り、終了意図や通知元を再確認します。

`CreateIndependentContext()` はアクセス管理だけを共有する別ルートを作ります。
CancellationObject と With で追加した値は継承しません。フレーム固有の FrameState / WindowState は
この別ルートに追加します。通常の `new FlowContext(parent)` は、従来どおり親を探索しキャンセルも共有します。

Context の Dispose は自分の参照を解放するだけで、共有アクセス管理を無効化しません。
片方を破棄しても他方や取得済みのアクセス契約は利用できます。Task のキャンセル・待機も行わないため、
実行所有者が全処理の終了を待つ責務は維持されます。Core / Standard の API は変更していません。

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
Count と ExitRequested は状態アクセス契約で取得専用として提供し、生の State は取得できません。

## スレッド・Context・終了契約

- `WindowSystemFlow` は同期的にフレームを進め、Open から Close まで呼び出しスレッドを維持します。
  `IWindowOperation` / `IWindowReaction` は同期完了する契約です。フレーム内で非同期描画を待ちません。
- カウンターだけを `Task.Run` で起動します。カウンターの Operation / Reaction は raylib を呼びません。
  フレームの描画完了も待たないため、終了時にメインスレッドがカウンターを同期的に待てます。
- フレーム用 Context は `CounterFlowContext.CreateIndependentContext()` で作る独立したルートです。
  親ありの FlowContext はキャンセルを共有するため、この分離には使いません。
  終了用の子 Context はフレームが終わった後に作ります。
- 両 Context が同じアクセス管理を共有します。ゲーム状態・表示・入力通知は分割されていますが、
  更新は同じ gate で管理し、入力受付表示と実際の受理条件は同じ判定から導出します。
- `ICounterDisplayAccess.Snapshot` は count / message / acceptingInput を同じロック内で out 引数へ取得します。
  描画側は取得済みの値を使い、その後の更新で描画中の値が変わることはありません。
- 入力待機はアクセス管理が CounterInputWait と協調して維持します。ロック内では await せず、継続は非同期実行します。
  Operation は Context から入力アクセスを取得して委譲し、通知や生の State への参照を保持しません。
- Context / Scope 一般の並行安全性を仮定していません。同じ Function が並行利用される部分では、
  入力・表示の共有状態を明示的に同期し、実行中に登録の変更や状態リセットを行いません。
  共有するアクセス管理は両フロー開始前に確定し、Context の登録変更・破棄と取得の並行実行は行いません。
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
