# サンプルでの `Result<T, TError>` の検討と実装記録

2026年11月17日。新 Result API への移行後、
[引き継ぎ文書](../../archives/result-api/RESULT_TYPED_ERRORS_SAMPLES_HANDOFF.md) に沿って
Notepad Secure 版、EscapeGame、Notepad 共通・通常版、Raylib Counter、Parrot の順に調べました。
各サンプルの調査結果をレビューし、採用した箇所は実装・検証結果もレビューしました。

## 採否の基準

失敗値を最初に読む呼び出し側と、その値で選ぶ処理を確認しました。
失敗値を使う目的として、回復経路の選択に加え、既に返された終了結果をチェーンの先へ運ぶことも検討しました。
取り消しや正常停止はアプリケーションの進行状態として扱い、診断には元の原因例外を残しました。
[Result API の契約](../RESULT.md) · [終了結果の契約](../REACTION_RESULTS.md)

## Notepad Secure 版

**採用。** アカウント情報が見つからない場合に、既存ノートを調べて登録の可否を決める分岐がありました。
読み込みを `LoadCredentialAsync(string user)` に切り出し、
`Task<Result<CredentialData, CredentialLoadError>>` を返すようにしました。
`FileNotFoundException` と `DirectoryNotFoundException` による返却失敗は `Missing`、
その他の返却失敗は `ReadFailed` として、元の `ResultException` と一緒に保持します。

`AuthenticateAsync` は `Missing` の場合にだけ同じユーザーの既存ノートを確認します。
ノートがあれば登録を拒否し、なければパスワードの再入力へ進みます。
`ReadFailed` では `WithoutValue` で原因を既存の `Task<Result>` に戻し、ログイン失敗の表示につなげます。
Port 契約は従来どおりで、読み込み Task の例外とキャンセルはそのまま伝播します。
[実装](../../InteractionFlow.Samples.Notepad.Secure/Externals/SecureLoginOperation.cs)

サンプルを起動するテストで、新規登録、既存アカウントでの再ログイン、既存ノートがある場合の登録拒否、
破損したアカウント情報とノートの保全、登録の取り消しを確認しました。
追加した境界テストでは、返却失敗のラッパーの同一性、送出例外と Task キャンセルの伝播、
ログイン切り替え時の以前のセッション鍵の破棄を確認しました。
コミットは `dca1782` です。
[実行テスト](../../InteractionFlow.Samples.Notepad.Tests/NotepadProcessTests.cs) ·
[境界テスト](../../InteractionFlow.Samples.Notepad.Tests/InteractionBoundaryTests.cs)

## EscapeGame

### 検討の経緯

最初に、正常停止を示す `SessionEndException` の判定を失敗値で置き換える案を検討しました。
この案では引き続き正常停止のために例外を生成し、後始末に渡す時点で成功に戻すため、当初は見送りとしました。

レビューで、子が返した `InteractionEnd` を失敗時にも保持する用途を調べ直しました。
当時の `GameplayAsync` は子の終了結果から `.Result` だけを引き継いでおり、
原因例外が残っても、後始末後に返るのは `EndSession` が生成した終了結果でした。
この境界で元の終了結果を保持し、後始末の結果に応じて返し分ける用途に採用を決めました。

`IGameplayContext` を失敗値にする案も比較しました。
途中で作られた Context を呼び出し側に渡し、その Context を使って復旧する契約なら、失敗値に含める意味があります。
このフローでは `PlayAndCheckContinueAsync` 自身が後始末に必要な Context を所有しています。
セッション処理から返す失敗値には、呼び出し側がまだ持っていない終了結果を載せました。
[Context の寿命](../../InteractionFlow.Samples.EscapeGame/Aspects/Gameplay/Entities/Contexts/GameplayContext.cs)

### 実装と検証

**採用。** `GameplayAsync` を `Task<Result<SessionProgress, SessionFailure>>` に変更しました。
`BeginSession` が返した `InteractionEnd` を `ThenAsync` で継続チェックに渡し、
完走と正常停止は成功値の `Completed` と `Stopped` で区別します。
失敗時には原因例外と `SessionFailure` を保持し、子が終了結果を返していればその `InteractionEnd` を格納します。
子が例外を送出した場合は原因例外を保持し、`SessionFailure.End` は空です。

`PlayAndCheckContinueAsync` は `ResolveAsync` で `EndSession` に接続します。
後始末が先行失敗を同じ `ResultException` で引き継いだ場合は、子の `InteractionEnd` をそのまま返します。
後始末にも失敗した場合は両方の原因を保持します。再試行は完走後、後始末が成功し、停止要求がない場合に限ります。
[セッションフロー](../../InteractionFlow.Samples.EscapeGame/SystemFlows/GamePlaySystemFlow.cs)

あわせて SystemFlow レイヤの Result チェーンを見直しました。
`WindowSystemFlow` は `RunContext` を廃止し、Context と子の Task を実行メソッドで管理します。
値なし `Result` と `FailureCollector` で実行から停止要求、Cancel、同期合流をつなぎ、
集約した失敗を Close に渡してウィンドウ全体の終了結果を返します。
返却失敗は `ThenErrorAsync` で収集し、送出例外は捕捉した箇所で収集器に直接渡します。
初回の検証では、送出例外を一度 `Result` に変換すると余分なラッパーが追加され、
集約内の例外の同一性が崩れることを確認しました。収集経路を修正し、既存の順序とインスタンスを保ちました。
`GameSessionSystemFlow` のターン反復は、停止時にそのターンの終了結果を直接返す実装を維持しました。
[ウィンドウフロー](../../InteractionFlow.Samples.EscapeGame/SystemFlows/WindowSystemFlow.cs) ·
[ターン反復](../../InteractionFlow.Samples.EscapeGame/SystemFlows/GameSessionSystemFlow.cs)

追加テストで、元の `ReactionEnd` の同一性と `EndSession` の実行が一度であることを確認しました。
既存テストで、失敗の集約順序、停止・合流・Close、ウィンドウの所有スレッド、
正常停止と無関係なキャンセルの扱い、リトライを確認しました。
Raylib 本体を起動し、初期画面が描画され、ゲーム内の Exit で終了コード 0 になることも確認しました。
コミットは `b110ada` です。
[フローテスト](../../InteractionFlow.Samples.EscapeGame.Tests/FlowTests.cs)

## Notepad 共通・通常版

**見送り。** `ReadKeyAsync` は選択済み・取り消し・候補なし・無効な ID を成功側の `ReadKeyValue` で区別します。
一覧取得・表示・選択の失敗は同じ `ExceptionPort` に委譲します。
読み込めないノートは一覧や選択候補で `[Unreadable]` と表示し、編集では `Load failed` を表示して止めます。
扱いは操作ごとに決まり、不存在や形式不正による分岐はありません。保存も失敗時に原因を表示する単一の経路です。
`MainLoop` は子の `InteractionEnd` を直接返します。シグネチャとコードの変更はありません。
[キー取得と保存](../../InteractionFlow.Samples.Notepad.Core/Interactions/NoteEditor.cs) ·
[通常版 Serializer](../../InteractionFlow.Samples.Notepad/Externals/NotepadDataSimpleSerializer.cs) ·
[メインループ](../../InteractionFlow.Samples.Notepad.Core/SystemFlows/MainLoop.cs)

## Raylib Counter

**見送り。** Open・描画・カウンターの失敗は集約して Close に渡します。
Close が描画資源を解放するかは `WindowState.IsOpen` で決まり、診断には集約した例外を使います。
`CounterSystemFlow` は子の終了結果を直接返し、`WindowSystemFlow` は Close の終了結果を返します。
Open 後の分岐を同期 `Then` に置き換えても、描画ループと `finally` による終了要求・Cancel・合流は残ります。
シグネチャとコードの変更はありません。
[ウィンドウフロー](../../InteractionFlow.Samples.Raylib/SystemFlows/WindowSystemFlow.cs) ·
[Close の実装](../../InteractionFlow.Samples.Raylib/Externals/RaylibWindowReaction.cs) ·
[所有スレッドと後始末のテスト](../../InteractionFlow.Samples.Raylib.Tests/WindowFlowTests.cs)

## Parrot

**見送り。** メニューで無効な入力と前回の選択がない場合は `SelectSample` が別々の案内を表示し、
SystemFlow はどちらもメニューを再表示します。空行・EOF は終了へ進み、Storage の失敗は例外処理へ渡ります。
会話中に表示が失敗した場合、`TalkAsync` は受け取った `ReactionEnd` を
`Result<ReactionEnd>` の成功値に載せて Scheduler 経由でそのまま返せます。
Scheduler 側の失敗は既存の例外・キャンセル処理へ渡り、`ParrotApplication` は終了時に子の `InteractionEnd` を直接返します。
シグネチャとコードの変更はありません。
[メニュー](../../InteractionFlow.Samples.Parrot/Interactions/SelectSample.cs) ·
[会話とタイムアウト](../../InteractionFlow.Samples.Parrot/Interactions/TalkToParrot.cs) ·
[アプリケーションフロー](../../InteractionFlow.Samples.Parrot/SystemFlows/ParrotApplication.cs)

## 最終確認

実装変更後に `dotnet build InteractionFlow.slnx` と `dotnet test InteractionFlow.slnx` を昇格付きで実行しました。
最終ビルドは警告・エラーともに 0 件で、EscapeGame の 156 件を含む全 649 テストが合格しました。
