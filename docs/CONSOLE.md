# Standard Console

`InteractionFlow.Standard.ConsoleSystem` は、Console の入力・表示・外部環境連携を提供します。
Core の変更や旧 Console API の互換レイヤーを必要としません。

## 配置と責務

フォルダーと名前空間は対応します。

| 配置（ConsoleSystem 以下） | 主な型 | 責務 |
| --- | --- | --- |
| Entities | ConsoleTextStyle / ConsolePosition | 不変の表示指定・座標 |
| ExternalPorts | IConsoleLineInputPort / IConsoleKeyInputPort | 入力 |
| ExternalPorts | IConsoleOutputPort / IConsoleScreenPort | 表示・画面変更 |
| ExternalPorts | IConsoleEnvironmentPort / IConsoleCancellationBindingPort | 環境の観測・キャンセル接続 |
| Externals | ConsoleLineInput / ConsoleKeyInput | 入力の実装 |
| Externals | ConsoleOutput / ConsoleScreen / ConsoleExceptionHandling / ConsoleCancellationHandling | 反応の実装 |
| Externals | ConsoleEnvironment / ConsoleCancellationBinding | 外部環境連携の実装 |
| Builders | ConsoleProfile | DI 登録 |

公開する機能は Port を通して利用します。標準実装は System.Console を直接使用します。
テスト用の内部デバイス抽象や、内部実装へのアクセスを許可する設定は設けません。

入力の占有・解放は Externals 内の実装基底 ConsoleInputBase が共有します。
テスト用の派生フックは持たせていません。
ConsoleOutput と ConsoleScreen は Core の Reaction を直接継承し、成功・失敗の結果変換を各実装に記述します。
例外・キャンセル通知は Core の専用基底を継承するため、出力手順だけを Externals 配下の
内部クラス ConsoleOutputWriter で共有します。
出力と画面操作を同期する ConsoleSynchronization は、プロセス共通のロックだけを保持します。

## 登録

```csharp
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.ConsoleSystem.Builders;

var builder = new ScopeBuilder();
builder.Apply(ConsoleProfile.Default);
```

プロファイルは六つの Console Port と、Core の `IExceptionPort<Exception>` / `ICancellationPort` を登録します。
登録やサービス解決だけでは入出力や Ctrl+C の購読を開始しません。
具象実装は sealed です。動作を変える場合は、既存の登録 API で Port の実装を差し替えます。

## 入力

- `ReadLineAsync(context)` は `string?` を返します。空文字は空行、null は EOF です。
- 標準の `Console.ReadLine` を Task.Run 内で実行し、端末側の行編集を利用します。
- キャンセル済みなら読み取りを開始しません。開始後に受理したキャンセルは保留し、
  読み取りが戻ってから OperationCanceledException として終了します。
- キャンセル後に Enter が必要になる場合があります。リダイレクト入力では行・EOF・読み取り失敗を
  待つ場合があり、終了までの時間上限はありません。
- 入力処理を残したままキャンセル完了にはしません。読み取り失敗は元の例外を伝播します。
- 読み取り成功後も、キャンセル可能な受付猶予を待ってから正常完了を確定します。
  ConsoleLineInput の CancellationGracePeriod は既定 100ms、コンストラクターで変更でき、ゼロなら追加待機しません。
  通常行・空行・EOF のいずれにも適用します。猶予中も入力の占有を保ちます。
- 読み取り中に既にキャンセルが要求されていれば、読み取り終了後に追加の猶予は待ちません。
  猶予中の要求でも待機を打ち切って OperationCanceledException にします。
- 猶予終了時の最終キャンセル確認を正常完了の確定点とします。
  それ以降の要求で返した入力結果を変更せず、Context のキャンセル状態を後続処理へ残します。
- `ReadKeyAsync(context)` はエコーせず ConsoleKeyInfo を返します。待機はキャンセル可能です。
  リダイレクト入力では NotSupportedException になります。
- 行・キーを合わせてプロセス内の入力は一件に限定します。キャンセル保留中も占有し、
  二重入力は InvalidOperationException で拒否します。

業務上のプロンプトや操作結果は Interaction が Reaction を使って表示します。
一方、入力の成立・終了に必要な案内は入力 Operation の責務です。
ConsoleLineInput は読み取り待機中にキャンセルを検知すると、標準エラーへ一度だけ
`Cancellation requested. Press Enter to cancel input.` と表示して、読み取り終了を待ちます。
入力がリダイレクトされている場合は Enter を求めず、現在の入力終了を待つ旨を表示します。
開始前キャンセル、読み取り完了後、受付猶予中にはこの案内を出しません。
キャンセル通知のコールバックは待機側への通知のみ行い、そのスレッドでは表示しません。
案内は他の標準出力・画面操作と排他します。表示に失敗しても読み取り終了まで待ち、
キャンセルまたは読み取り失敗と表示失敗を AggregateException に保持します。

### 行入力のキャンセル受付と待機

| 条件 | 待機と結果 |
| --- | --- |
| 開始前にキャンセル済み | 読み取りを開始せずキャンセル |
| 読み取り中に要求、ReadLine はまだ戻らない | 終了方法を案内し、読み取り終了を待つ |
| 読み取り終了時にキャンセル済み | 追加の猶予を待たずキャンセル |
| 読み取りが先に戻り、猶予中に通知 | 待機を打ち切り、今回の入力をキャンセル |
| 猶予終了まで要求なし | 行・空行・EOF を正常に返す |
| 正常完了の確定後に要求 | 返した結果は変更せず、次の処理にキャンセル状態を引き継ぐ |
| 読み取り自体が失敗 | 猶予を待たず元の失敗を伝播。キャンセル状態も勝手にリセットしない |

Ctrl+C をこの契約へ接続するには IConsoleCancellationBindingPort.Bind が必要です。
独自の CancelKeyPress 購読だけで対象のキャンセル状態を変更しない場合は、キャンセル要求にはなりません。
トークン経由の要求にも同じ受付期間を適用します。

ReadLine は戻った原因を返さず、CancelKeyPress の通知にも時間上限はないため、
猶予期間を超えたイベントを今回の入力に必ず対応付けることは保証しません。
100ms は遅延通知を受け付けるための既定値であり、通知順序を保証する同期処理ではありません。
通知到着前に接続を解除した場合も、まだ届いていないイベントを読み取り側で認識することはできません。

## 出力と画面操作

`WriteAsync` / `WriteLineAsync` は文字列を標準出力へ書き込み、ReactionEnd を返します。
null の文字列は引数エラー、出力・復元の失敗は失敗 ReactionEnd です。
キャンセル通知にも使えるよう、context のキャンセル状態を理由に出力を抑制しません。

`ConsoleTextStyle` は nullable な Foreground / Background を持ち、default は色の変更なしです。
指定した色は一回の出力にだけ適用し、失敗時も元へ復元します。出力と復元の両方が失敗した場合は
AggregateException に保持します。リダイレクトされた出力では色を操作しません。

`ClearAsync` / `SetCursorPositionAsync` は表示を変える Reaction です。
非対応環境や無効な画面位置は失敗結果になります。環境の観測は SilentExternal が担当します。
位置は非負の Left / Top を持つ ConsolePosition とし、現在位置の取得は Result を返します。
`IConsoleEnvironmentPort.GetWindowSize()` は表示ウィンドウの列数・行数を
`Result<(int Width, int Height)>` で返します。画面操作と同じロックで観測し、
リダイレクト出力や取得失敗は失敗 Result とします。サイズ変更通知ではなく、呼び出し時点の観測値です。

色の退避から復元までと画面操作はプロセス内で排他します。
外部コードによる System.Console の直接操作、入力中の通知による行の再描画、TUI の表示整合性は保証しません。

## Ctrl+C の接続

Interaction が `IConsoleCancellationBindingPort` を受け取り、`Bind(context)` と `Unbind()` を呼びます。
短い実行範囲なら try/finally で対応させ、複数 Interaction をまたぐ場合は終了経路にも解除を設計します。
接続した Port インスタンスを保持する DI Scope は、接続を必要とする間生存させてください。

- 接続先の CancellationObject だけを保持し、Context の所有権は取得しません。
- 接続中の Ctrl+C は既定終了を抑制し、対象へキャンセルを要求します。
- 同じ対象への Bind は何も増やしません。接続中の対象変更、別インスタンスによる同時接続は拒否します。
- Unbind / ForceResetMemoryState は購読と接続先を解除し、再接続できます。
- 実装の IDisposable により、DI Scope 終了時にも解除します。破棄後の Bind は拒否します。
- 解除は進行中のキャンセル通知と同期します。同期キャンセルコールバックからの解除も可能です。
  キャンセルコールバックから別スレッド上の解除を同期的に待つことは避けてください。

接続は読み取りを強制中断する仕組みではありません。Ctrl+C で ReadLine が戻るかどうかは端末によります。

## 例外・キャンセルの通知

例外は標準エラーへ表示し、元の例外を失敗結果として保持します。
キャンセルは Core の待機・リセット後に通知し、処理に成功すれば成功結果を返します。
これは現在のキャンセルを処理したという意味で、アプリ全体の終了指示ではありません。
待機・リセットや通知が失敗した場合は、元のキャンセルを含む失敗結果を返します。

## 検証

実機検証には [ConsoleSystem.ManualTests](../InteractionFlow.Standard.ConsoleSystem.ManualTests/README.md) を使えます。
メニューまたはコマンド引数で行入力、キー入力、Ctrl+C、タイマー経由のキャンセル、色の復元を確認できます。

自動テストは `InteractionFlow.Standard.Tests/ConsoleSystem` にあります。

- ConsolePortTests: Port に対する実装の差し替えで、利用側 Interaction を検証します。
- ConsoleIntegrationTests: 標準実装をそのまま使用し、公開の Console.SetIn / SetOut / SetError で
  標準ストリームを設定して、入力の保留・失敗、入出力、排他、通知失敗を検証します。
  ストリームは finally で必ず復元します。接続の占有・解除・DI Scope の破棄は公開 API で確認します。

実端末の色の適用・復元、キー入力、Ctrl+C のイベント配送と解除との競合、IME、貼り付けは
対象 OS・端末で別途確認してください。内部のイベントハンドラーをリフレクションで呼ぶテストは行いません。
Parrot と Notepad は新 Console API に移行済みです。選択・会話・自動入力・色指定・キャンセルの利用例は
[Parrot](../InteractionFlow.Samples.Parrot/README.md)、カーソル編集・矢印選択とパスワードのキー入力は
[Notepad Core](../InteractionFlow.Samples.Notepad.Core/README.md) と
[Notepad Secure](../InteractionFlow.Samples.Notepad.Secure/README.md) を参照してください。
