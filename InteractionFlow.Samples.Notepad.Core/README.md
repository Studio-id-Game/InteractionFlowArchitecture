<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリ実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |

---

# Notepad Core

通常版と Secure 版が共有する、Console メモ帳のモデル・Port・Interaction・SystemFlow です。
ライブラリの Core / Standard API は変更せず、新しい ConsoleSystem / FileSystem / SerializationSystem を利用します。

## 機能と構成

- `NotepadData`: タイトルと本文を持つ不変のノート。編集では新しい下書きを作り、確定まで既存値を変更しません。
- `NotepadRules` / `NotepadKey`: ID の検証・正規化、Public と名前付きユーザーの識別。
- `NotepadContext`: 認証済みユーザー、次の操作、終了意図を保持します。未ログインは User が null です。
- `Login` / `SelectUserAction` / `NoteListView` / `NoteCreate` / `NoteEdit` / `NoteDelete`: 相互作用ごとの処理。
- `MainLoop`: ログイン、一覧、操作選択を合成します。通常版・Secure 版で同じ Flow を使います。
- `INotepadLoginOperation`: ユーザー入力・認証・Context 更新の差し替え点。
- `INotepadDataPersistencePort` / `INotepadDataSerializerPort`: 永続化と形式変換の差し替え点。
- `NotepadDataFilePersistence`: Standard の FilePersistence に ID とパスの変換を追加します。

`NotepadInteraction` は Ctrl+C の Bind / Unbind とプロンプトの失敗確認を共通化する小さな基底です。
`NoteEditor` は作成・編集で重複する ID 選択と編集 Operation の呼び出しを共有します。
`ConsoleChoiceOperation` は矢印キーの選択、`ConsoleNoteEditor` はカーソル編集を担当します。
`NoteDraft` は文字列とカーソルの更新規則を持ち、ファイルや Console に依存しません。
端末サイズは Standard の `IConsoleEnvironmentPort.GetWindowSize()` で取得します。
共通登録は `IScopeProfile` を実装した `NotepadProfile.Default` を `Apply` して利用します。
これらはサンプル内の補助であり、汎用的なフレームワークやライブラリ API の追加ではありません。
一覧は保存ファイルから取得します。別のユーザー一覧インデックスや可変キャッシュは持ちません。

## 操作

起動・保存先については [通常版](../InteractionFlow.Samples.Notepad/README.md) または
[Secure 版](../InteractionFlow.Samples.Notepad.Secure/README.md) を参照してください。

### 対話端末（通常の起動）

1. ユーザー ID を入力します。空行は Public、`/exit` または EOF は終了です。
2. メニューは矢印キーで移動し、Enter で確定します。Home / End、PageUp / PageDown も利用できます。
3. 編集・削除する既存ノートも、ID とタイトルの一覧から矢印キーで選択します。Esc で取り消します。
4. 削除確認は No / Yes の選択です。初期選択は No です。
5. List はキー入力まで一覧を表示します。
6. メニューの Exit または Esc・Ctrl+C による終了要求を受け、MainLoop がアプリを終了します。

作成・編集は同じ画面を使います。先頭行の `T |` がタイトル、その下が本文です。
既存ノートは現在のタイトル・本文を読み込んだ状態で編集できます。

| キー | 操作 |
| --- | --- |
| 矢印キー | 文字・行の移動 |
| Home / End | 行頭・行末へ移動 |
| Ctrl+Home / Ctrl+End | タイトル先頭・本文末尾へ移動 |
| PageUp / PageDown | 画面単位で上下移動 |
| Enter | 本文の行を分割。タイトル行では本文先頭へ移動 |
| Backspace / Delete | 文字を削除。本文の行境界では行を結合 |
| Tab | 空白4文字を挿入 |
| F2 / Ctrl+S | 下書きを確定して保存 |
| Esc / Ctrl+C | 下書きを破棄してメニューへ戻る |

画面外の行・長い行は縦横にスクロールします。タイトルと本文は別フィールドとして保持し、
Backspace / Delete によって両者を結合することはありません。
日本語と絵文字の幅を考慮し、文字要素単位で移動・削除します。
一部の絵文字や曖昧幅文字の表示幅は端末設定により異なります。
端末は24列・8行以上で使用してください。Ctrl+S を端末が捕捉する場合は F2 を使います。

ユーザー ID とノート ID は英字・数字・`_`・`-` の1〜64文字です。
前後の空白を除き、小文字に統一します。タイトル・本文は日本語や絵文字を利用できます。
Public と名前付きの `public` ユーザーは別の保存先です。
タイトルは200文字、本文は1,048,576文字までです（.NET の UTF-16 コード単位）。
作成時のノート ID の空行は操作を中断します。

### リダイレクト入力

入力または出力がリダイレクトされているときは、カーソル制御せず行入力を使います。
メニューは番号または名前、ノート選択は番号または ID を入力します。0・空行で選択を取り消します。
メニューでの取り消しはアプリ終了の要求です。削除確認は `yes` または `no` を入力します。

本文は `.` だけの行で保存し、`..` で `.` 行を入力します。`/cancel` は下書きの破棄です。
編集時の空タイトルは現タイトルを保持し、本文の最初の `/keep` は現在の本文を保持します。
これらのコマンド文字列は行入力専用で、カーソル編集時には通常の文字として入力できます。

### 中断と失敗

編集中の Ctrl+C は保存せずメニューへ戻ります。行入力中の EOF は下書きを破棄して終了を要求します。
ログイン中の Ctrl+C も終了を要求します。標準行入力では Ctrl+C 後に Enter が必要な場合があります。
終了要求と操作結果を受け、体験の継続・終了は MainLoop が判断します。
出力・画面操作の失敗は操作を中断し、Interaction 基底の例外 Port に委譲します。
保存失敗は案内してメニューへ戻り、Standard FilePersistence が既存ファイルを保持します。

## 旧実装との違い

旧コード50ファイル（3プロジェクトの bin / obj を除く）は
[退避 ZIP](../archives/Notepad-before-console-system.zip) に保存し、元の内容とのバイト一致を確認しました。
対話端末での矢印選択・カーソル編集を新しい ConsoleSystem Port で実装しています。
リダイレクト入力には番号メニュー・行編集を用意しています。
旧データ形式の自動移行は行いません。新しい保存先は旧 NotepadData と分離しています。

## 検証

```powershell
dotnet test InteractionFlow.Samples.Notepad.Tests/InteractionFlow.Samples.Notepad.Tests.csproj
```

通常版・Secure 版の実プロセスによる CRUD・再起動・日本語・ユーザー分離・入力中断と、
Port 差し替えによる矢印選択・カーソル編集・スクロール・キャンセル・画面失敗、
Serializer と実ファイルの保存失敗を検証します。
擬似端末で通常版の矢印選択、日本語入力、F2 保存、再編集、Esc による破棄と終了を確認しています。
実端末の IME、Ctrl+C イベント配送とパスワードのマスク表示は別途手動確認の対象です。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |
