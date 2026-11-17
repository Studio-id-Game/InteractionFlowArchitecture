# サンプルでの `Result<T, TError>` 利用の引き継ぎ

2026年11月17日、基準コミットは `61fd6a7` です。新 Result API への移行と旧 API の削除は完了しています。
次の作業では **Parrot、Notepad（共通・通常版・Secure 版）、Raylib Counter、EscapeGame** を対象に、
呼び出し側が失敗値を使う箇所を見つけ、採用しやすい箇所から一サンプルずつ実装・検証します。
対象範囲から HelloDoor と HelloDoor.Lock を外します。
この文書の候補は採否を決めるための出発点です。
[移行記録](../../docs/RESULT_API_MIGRATION.md) · [Result API の契約](../../docs/RESULT.md)

実施後の採否、設計レビューでの再検討、実装・検証結果は
[サンプルでの検討と実装記録](../../docs/design-notes/RESULT_TYPED_ERRORS_SAMPLES_REVIEW.md) にまとめました。

## 採用判断の進め方

1. 既存コードで、失敗を判定する箇所と、その判定から実際に選ばれる処理を確認します。
   回復、再試行、診断表示などで呼び出し側が失敗の区分や追加情報を使う場合に、`TError` を候補にします。
2. 候補ごとに、成功値 `T`、失敗値 `TError`、保持する原因例外、失敗値を最初に読むメソッドを決めます。
   そのメソッドが選ぶ処理と、失敗値を外側の契約に渡す範囲も確認します。
   例外だけで処理が決まる場合は、既存の Result を使います。
3. 状態と Context、SystemFlow、Interaction、Port 契約の順に影響を確認します。
   サンプル内の小さな境界で使い、Port の戻り値を変更する場合は実装・呼び出し側・テストダブルを一緒に更新します。
   基本ルールへの抵触や大きな設計変更が必要と分かった段階で、影響と理由を報告して実装を止めます。
   [エージェントガイド](../../AGENTS.md) · [アプリ設計ガイド](../../docs/guides/INTERACTIONFLOW_APPLICATION_DESIGN.md)

## 新 API の使い方と境界

- 失敗は `Result<T, TError>.CreateFailure(exception, detail)` または `(exception, detail)` で作ります。
  失敗値には、分岐に使える列挙値や小さなデータ型を使えます。成功値・失敗値に `null` や例外のインスタンスは渡せません。
  値付き Result の `default` は検査時に `ResultException` を送出します。
- 失敗値は `HasError(out var error, out var detail)` や `ThenError` / `Resolve` の失敗コールバックで取り出します。
  既存の `ResultException` を失敗結果へ渡すと原因のラッパーを引き継げます。診断のための元の例外と、分岐に使う失敗値を両方扱えます。
- `Then` で成功値の型を変える場合、同じ `TError` を使うチェーンなら失敗値も引き継げます。
  既存の `Result<T>` から失敗値付きへ移す箇所では、失敗を読む処理で `CreateFailure` を使って分類します。
  例外だけを受け取る境界へ戻る際は、失敗値を消費したうえで `WithoutErrorValue` や `WithoutValue` を選びます。
  [Result の型定義](../../InteractionFlow.Core/ResultAPI/Entities/Result.cs) · [生成対象](../../InteractionFlow.Core/ResultAPI/Entities/ResultTemplate.cs)
- 通常終了、ユーザーの取り消し、返却された失敗、送出例外、Task のキャンセルは既存の意味に沿って扱います。
  `ReactionEnd` / `InteractionEnd` を返す箇所では、終了結果の同一性や失敗の集約を確認します。
  復帰先に要件のある待機点だけで `ConfigureAwait` を明示し、描画スレッドの確認には `WindowState` と実行時のテストを使います。
  [Reaction とフローの結果](../../docs/REACTION_RESULTS.md) ·
  [描画ライブラリとの接続設計](../../docs/guides/RENDERING_LIBRARY_INTEGRATION.md)

## サンプル別の検討候補

### Notepad 共通・通常版

`NoteEditor.ReadKeyAsync` は `Result<ReadKeyValue>` を返し、選択済み・取り消し・候補なし・無効な ID は成功側の値で区別しています。
一覧取得・表示・選択の失敗は例外を伝え、呼び出し側が同じ例外処理に委譲します。
まずこの区別を保ち、読み取り失敗を呼び出し側が実際に区別して扱う箇所があるかを確認してください。
通常版の Serializer とファイル保存は Standard の契約を使っています。
分類が必要なら、表示や回復を決めるサンプル側の処理で行う案から検討します。
[キー取得](../../InteractionFlow.Samples.Notepad.Core/Interactions/NoteEditor.cs) ·
[通常版の Serializer](../../InteractionFlow.Samples.Notepad/Externals/NotepadDataSimpleSerializer.cs) ·
[境界テスト](../../InteractionFlow.Samples.Notepad.Tests/InteractionBoundaryTests.cs)

### Notepad Secure 版

`SecureLoginOperation.AuthenticateAsync` は、アカウント情報の読み込み失敗のうちファイル・ディレクトリの不存在を判定し、登録経路に進めるかを決めています。
この読み込みだけを小さな補助処理に切り出し、成功値を `CredentialData`、失敗値を「不存在」「その他の読み込み失敗」などの区分とする案が最初の候補です。
認証側でその区分を消費し、既存ノートがある場合の登録拒否や元の例外を保てるかを確認します。
失敗値を導入することで呼び出し側の判断が明確になる場合に採用します。
認証失敗時に以前のセッション鍵を破棄する契約も検証対象です。
[認証と登録](../../InteractionFlow.Samples.Notepad.Secure/Externals/SecureLoginOperation.cs) ·
[セッション実装](../../InteractionFlow.Samples.Notepad.Secure/Externals/SecureSession.cs) ·
[Secure 版のテスト](../../InteractionFlow.Samples.Notepad.Tests/SerializerTests.cs)

### EscapeGame

`GamePlaySystemFlow` は `Result<IGameplayContext>` を連結し、`SessionEndException` を見て正常停止として `EndSessionAsync` に成功を渡しています。
既存の例外型による識別を、局所的な失敗値で「チェーンを打ち切る正常停止」と「処理失敗」に分けて `ResolveAsync` で消費する案を検討できます。
正常停止を成功側の状態として表す案も比較し、型と分岐が素直に読める方を選びます。
どちらでも、無関係なキャンセルと本来の失敗を保持し、EndSession を一度だけ行うことを確認します。
既存の終了トークンを直接返す `PlayTurn` / `HandleResult` や、停止・合流・Close を担うウィンドウ側への変更は、必要性が確認できた場合に範囲を広げます。
[セッションフロー](../../InteractionFlow.Samples.EscapeGame/SystemFlows/GamePlaySystemFlow.cs) ·
[ウィンドウフロー](../../InteractionFlow.Samples.EscapeGame/SystemFlows/WindowSystemFlow.cs) ·
[フローテスト](../../InteractionFlow.Samples.EscapeGame.Tests/FlowTests.cs)

### Raylib Counter

`WindowSystemFlow` は Open、フレーム、カウンターの失敗を集約し、終了用 Context の値なし Result を Close に渡しています。
失敗段階によって Close 側の処理や通知が変わる必要があるかを確認してください。
現状どおり集約した例外を使うなら、失敗値を追加する利点は小さいです。
描画 Port の同期完了、Open から Close までの所有スレッド、カウンターのキャンセル・合流・Close の順序を維持します。
[ウィンドウフロー](../../InteractionFlow.Samples.Raylib/SystemFlows/WindowSystemFlow.cs) ·
[所有スレッド・後始末のテスト](../../InteractionFlow.Samples.Raylib.Tests/WindowFlowTests.cs)

### Parrot

メニューでは無効な選択と前回モード未登録をそれぞれ表示し、空行や EOF は終了に使っています。
選択解決を局所的な結果として分ける場合は、既存の表示分岐が失敗値を使うかを確認します。
`TalkToParrot` は `Result<ReactionEnd>` を `CancellationScheduler.TimeoutAsync` へ渡し、外側の失敗を例外・キャンセル処理へ戻しています。
Scheduler の現行オーバーロードは `Task<Result>` と `Task<Result<T>>` を受け取ります。
型付きの失敗値を試す場合は、メニューの局所処理から検討し、Scheduler をまたぐ必要が生じた場合は設計上の変更として扱います。
[メニュー選択](../../InteractionFlow.Samples.Parrot/Interactions/SelectSample.cs) ·
[会話とタイムアウト](../../InteractionFlow.Samples.Parrot/Interactions/TalkToParrot.cs) ·
[Scheduler の契約](../../docs/CANCELLATION_SCHEDULER.md)

## 実装順と完了の確認

最初に Notepad Secure 版のアカウント読み込みを検討し、次に EscapeGame のセッション終了を確認します。
Notepad 共通・通常版、Raylib Counter、Parrot は、それぞれ実際に失敗値を使う呼び出し側が見つかった箇所を実装候補にします。

- サンプルごとに、採否、変更したシグネチャ、失敗値を消費する処理、維持した既存の動作を記録します。
- 採用した箇所は、成功時、既知の失敗値による分岐、原因例外の保持を確認します。該当する場合は取り消し、送出例外、Task のキャンセル、既存データの保全も確認します。
- 変更後はリポジトリの定型フローに従い、`dotnet build InteractionFlow.slnx` と `dotnet test InteractionFlow.slnx` を昇格付きで実行します。
  コンソールの操作や描画・終了に影響する場合は、対象サンプルを実行して確認します。
