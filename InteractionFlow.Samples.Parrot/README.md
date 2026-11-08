<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリ実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |

---

# Parrot

新しい Core / Standard ConsoleSystem を使った、おしゃべりするオウムのサンプルです。

```powershell
dotnet run --project InteractionFlow.Samples.Parrot
```

番号（0〜5）または名前でモードを選択します。名前の大文字・小文字は区別しません。

| 番号 | モード | 動作 |
| --- | --- | --- |
| 0 | Parrot | 入力を3回繰り返します。`Parrot` を含むと6回、興奮して返します。 |
| 1 | ParrotAuto | 専用 Operation が自動入力し、Ctrl+C まで話し続けます。 |
| 2 | ParrotAutoAndKill | 自動入力を10秒でキャンセルし、メニューへ戻ります。 |
| 3 | ParrotColorful | オウムの出力に Cyan を指定します。出力ごとに元の色へ復元します。 |
| 4 | ParrotCustomContext | 会話のスコープに専用の挨拶を追加します。 |
| 5 | RepeatLast | 最後に選んだモードを再実行します。初回は案内して選び直します。 |

会話中の空行・空白行・EOF・Ctrl+C はメニューへ戻り、メニューでの同じ操作はアプリを終了します。
EOF の場合は、戻ったメニューでも EOF を受け取り終了します。
標準行入力の契約により、Ctrl+C 後に Enter が必要な場合があります。
自動入力はコンソールを読み取らないため、空行では終了せず Ctrl+C を使います。
色は実端末で確認してください。リダイレクト出力では色を操作しません。

## 構成

- `ParrotRules`: 選択文字列の解釈と、オウムの返答を作る純粋なルール。
- `SelectSample`: メニュー表示・選択・前回選択の保存。キャンセル時には終了意図を保持します。
- `TalkToParrot`: 挨拶・入力・文字送り表示。標準入力と自動入力を同じ Port 契約で扱います。
- `ParrotApplication`: 選択と会話の Interaction を順に実行し、継続と終了を判断します。
- `LastSelectMemory`: アプリの DI Scope に保持する Storage。再実行するモードだけを保存します。
- `AutomaticLineInput`: Console 入力を消費せず、自動で文字列を返す Operation。

選択状態・モード・専用挨拶は `ScopedFlowContext` の実行範囲へ閉じます。
表示は `IConsoleOutputPort` に委譲し、各 `ReactionEnd` の失敗で後続の入力や表示を停止します。
Ctrl+C は各 Interaction の `using var bindingScope = binding.Bind(context)` で接続期間を示します。
10秒の期限には `context.Cancellation.GetScheduler()` と `TimeoutAsync` を使い、通知後も会話 Task の終了を待ちます。
Scheduler は会話の実行範囲で破棄し、Interaction 全体の ExecuteAsync を待機対象にはしません。
`TalkAsync` は `Result<ReactionEnd>` を返し、`TimeoutAsync<T>` へ直接渡します。
時間制限モード以外は `Timeout.InfiniteTimeSpan` として同じ経路を使います。
ReactionEnd はそのまま成功値として返し、Reaction 内部の失敗は再処理せず保持します。
通知失敗は会話の結果より優先する致命的な失敗として扱い、その場合は会話の ReactionEnd を返しません。
外側の失敗時は ResultException のラッパーを除いた原因を再送出し、Interaction 基底の
例外・キャンセル処理へ戻します。成功時は会話の ReactionEnd をそのまま返します。
キャンセルの通知とリセット、未処理例外の通知は Interaction 基底と標準 Port に委譲します。
旧 Console の可変 State、ダミー入力 API、独自 RefEntity は使用しません。

旧コード一式（bin / obj を除く）は [退避ZIP](../archives/Parrot-before-console-system.zip) に保存しています。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |
