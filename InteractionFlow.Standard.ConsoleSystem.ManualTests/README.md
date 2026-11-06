# ConsoleSystem 実機検証

新しい Console Port の標準実装を実端末で操作する実行プロジェクトです。
自動テスト用のモックや内部の差し替え口は使用しません。
本体を直接観測するためのハーネスであり、製品の Interaction / SystemFlow の構成例ではありません。

## 起動

Windows Terminal、PowerShell、cmd などの通常の端末で、リポジトリのルートから実行します。
メニューから試す場合:

```powershell
.\InteractionFlow.Standard.ConsoleSystem.ManualTests\Run.ps1
```

このスクリプトはリポジトリの global.json で指定した .NET 10 SDK で対象プロジェクトだけをビルドします。
.NET 10 ランタイムで実行し、実際に使用した OS・ランタイムは起動時に表示します。

リポジトリ指定の SDK / ランタイムがある場合は、通常の起動も可能です。

```powershell
dotnet run --project InteractionFlow.Standard.ConsoleSystem.ManualTests
```

Visual Studio では、このプロジェクトをスタートアップに指定して Ctrl+F5 で起動できます。
IDE の出力欄ではなく、キー入力可能なコンソールで確認してください。
自動化ツール上の疑似端末は Ctrl+C をセッション終了として扱う場合があります。

## 確認する条件

| メニュー | 操作 | 見る結果 |
| --- | --- | --- |
| 1 | 通常の文字列、空行、IME 変換、貼り付けを Enter で確定 | SUCCESS。空行は `line=""`、EOF は `line=null` |
| 1 | 入力待ち中に Ctrl+C。待機が残るなら Enter | TOKEN の記録後、読み取りが戻った時点で CANCELED |
| 2 | Enter 直後、3秒以内に Ctrl+C | 受付猶予中の要求として CANCELED |
| 3 | 1.5秒以上入力せず待ち、その後 Enter | タイマーでキャンセルされ、待機中なら Enter を促す案内。Enter 後に CANCELED |
| 4 | 開始する | 入力を消費せず CANCELED |
| 5 | 1回目をキャンセルし、次の入力で別の文字列を確定 | 次の読み取りを開始でき、残った読み取りに入力を奪われない |
| 6 | 通常キー、修飾キー、Ctrl+C をそれぞれ試す | 通常キーはエコーなしでキー情報。Ctrl+C は CANCELED |
| 7 | 色付き出力の前後を確認 | 指定した色だけを適用し、次の通常行で元の色へ戻る |

各読み取り後は既定300msだけ接続を保って遅延通知を観測します。
Enter の案内は ConsoleLineInput 自身が標準エラーへ表示します。
受付猶予中のキャンセルでは追加の Enter は不要なので、この案内が出ないことも確認してください。
結果の後に `AFTER OBSERVATION canceled=True` なら、正常完了後にキャンセル要求が届いています。
返した結果をキャンセルへ変更せず、Context に要求が残ることを確認できます。
Context はリセットせず、次の試行には新しい Context を使います。
メニュー待ちや `--no-bind` の Ctrl+C は既定のプロセス終了になることがあります。

## コマンドで条件を指定する

```powershell
# Enter 後の長い受付猶予中に Ctrl+C を試す
.\InteractionFlow.Standard.ConsoleSystem.ManualTests\Run.ps1 line --grace-ms 3000

# Ctrl+C 接続なし。タイマーによるキャンセル後、必要なら Enter を押す
.\InteractionFlow.Standard.ConsoleSystem.ManualTests\Run.ps1 line --no-bind --cancel-after-ms 1500

# 受付猶予の途中でトークン経由のキャンセルを受ける。開始後すぐに Enter を押す
.\InteractionFlow.Standard.ConsoleSystem.ManualTests\Run.ps1 line --grace-ms 3000 --cancel-after-ms 1000

# 正常完了後の遅延要求を観測する。開始後すぐに Enter を押す
.\InteractionFlow.Standard.ConsoleSystem.ManualTests\Run.ps1 line --grace-ms 0 --cancel-after-ms 1000 --observe-ms 2000

# キー入力を3回。ログをファイルへ追記する
.\InteractionFlow.Standard.ConsoleSystem.ManualTests\Run.ps1 key --reads 3 --log console-key.log

# すべての引数
.\InteractionFlow.Standard.ConsoleSystem.ManualTests\Run.ps1 --help
```

`line` / `key` のオプション:

| 引数 | 意味 |
| --- | --- |
| `--grace-ms N` | 行入力の受付猶予。既定100、範囲0〜60000ms |
| `--cancel-before` | 各入力の開始前にキャンセル |
| `--cancel-after-ms N` | 各入力の開始時から N ms 後にトークンをキャンセル |
| `--no-bind` | Ctrl+C とキャンセルを接続しない |
| `--reads N` | 新しい Context で N 回実行。既定1、上限100 |
| `--observe-ms N` | 結果後の通知観測期間。既定300、範囲0〜60000ms |
| `--log PATH` | 終了後にイベント記録をファイルへ追記。親ディレクトリは事前に作成 |

`output` は `--log` のみ受け付けます。
未発火のキャンセルタイマーは観測期間の終了時に停止し、購読も解除します。
そのため `--cancel-after-ms` が読み取りと観測期間の合計より長ければ、要求は発生しません。

## ログの見方と制約

イベントハンドラーは画面へ出力せず、メモリーに記録します。イベント記録は各実行の最後に表示します。
結果には読み取り開始からの時間、通常行・EOF・キャンセル・例外、Context のキャンセル状態を記録します。
CancelKeyPress の観測ハンドラーは接続実装より先に登録し、Cancel フラグを変更しません。
診断コード自体の実行時間も含まれるため、記録は厳密な OS の発生時刻ではありません。

ログの `CALL BEGIN` は Port 呼び出し直前、`SUCCESS` / `CANCELED` は呼び出しから戻った時点です。
標準 ReadLine が内部で戻った瞬間を観測するものではありません。
ログは実行終了後に保存するため、OS による強制終了時はファイルに残りません。
入力文字列も記録するので、検証用の文字列を使用してください。

終了コード0は検証操作の完了を意味し、合格の自動判定ではありません。
API の予期しない例外や出力失敗は1、引数なしで入力がリダイレクトされている場合は2です。
プログラム全体の強制終了は別の終了コードになる場合があります。

リダイレクト入力も確認できます（通常の dotnet 起動が可能な環境の場合）。

```powershell
@('hello', '') | dotnet run --project InteractionFlow.Standard.ConsoleSystem.ManualTests -- line --reads 3 --observe-ms 0
```

この例では文字列・空行・EOF の順になります。キー入力はリダイレクト非対応なので FAILURE になります。
ReadLine は OS の入力を強制中断しません。Ctrl+C と Enter が必要になるかは端末によります。
受付猶予を超える通知まで、今回の入力へ対応付ける保証はありません。
