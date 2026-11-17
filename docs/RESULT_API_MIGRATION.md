# 新 Result API への移行記録

2026年11月15日に新 Result API の試作を始め、16日にジェネレーターを実装し、17日に旧 API からの切り替えを完了しました。
最初の試作は `ece62c9`、利用側の移行を計画する基準は `416ff74`、型の配置をそろえた最終コミットは `fe8a410` です。
現在の契約と使い方は [Result API](./RESULT.md) を参照してください。

## 開発の前提と背景

開発のきっかけは、EscapeGame で Result を使う中で見えた API の不足と、
型や待機方法の組み合わせごとにオーバーロードを手書きで保守する負担でした。
旧 API では、成功値の変換に `Map`、同期 Result から非同期処理に進む際に `StartAsync` や `AsResultAsync` を使っていました。
11月11日から14日には、EscapeGame で Result チェーンの適用範囲を検討しました。
局所検証と SystemFlow で Result を使い、`PlayTurn` と `HandleResult` では、
失敗・停止時に既存の終了トークンを返す経路を採用しました。
この先行検討では、チェーン化しても終了処理の順序、先行失敗、終了トークンの同一性を保つ必要がありました。
[EscapeGame の Result 利用調査](../archives/escape-game/ESCAPE_GAME_RESULT_API_REVIEW.md)、
[Result チェーン再設計プラン](../archives/escape-game/ESCAPE_GAME_RESULT_CHAIN_PLAN.md)

新 API の試作は、値なし、成功値付き、成功値と失敗値付きの3種類に、共通の分岐規則を適用する設計から始まりました。
型の組み合わせごとに `Then` や `ThenError` の本体を記述する代わりに、
分岐を C# のテンプレートに記述し、型の組と同期・`Task`・`ValueTask` の違いを生成器で展開する構成を選びました。
成功値や失敗値の検査、例外の保持、暗黙変換など、Result 自体の実行時契約は Core の手書きコードに置きました。

## ジェネレーター開発

### 試作と生成規則の設計

11月15日の `ece62c9` で、旧 API と共存する `InteractionFlow.Core.ResultAPI` に新しい Result 型、属性、テンプレートを置きました。
テンプレートの `Self` は拡張メソッドの受け手、`Other` は変換先を表します。
たとえば `Then` は成功時にコールバックを呼び、失敗時は変換先の失敗結果を作ります。
生成先は、同期版・Task 版・ValueTask 版の3つの `static partial` クラスです。

16日には、Result 型の指定を `ResultAPITarget` に集約し、`d761fb1` で生成規則と実装順を文書化しました。
変換候補は、引き継ぐ値や例外を受け取れるファクトリが成立する場合に絞りました。
同じ呼び出しに対して同じ処理になる候補はまとめ、曖昧なファクトリや互換性のない手書き宣言には診断を出す方針を定めました。
[テンプレート](../InteractionFlow.Core/ResultAPI/Entities/ResultTemplate.cs)、
[ジェネレーターの設計とロードマップ](../archives/result-api/RESULT_API_GENERATOR_PLAN.md)

### 生成器の実装と検証

`3c87549` で、Roslyn 4.8 の増分ソースジェネレーターを独立したプロジェクトとして追加し、Core のビルド時に参照する構成にしました。
既存の Analyzer は Roslyn 3.3.1 を参照していたため、ジェネレーターは別プロジェクトに置きました。
`67b033a` から `158c5bb` では、検出・解析・生成・出力の境界を定め、先に仮の処理でフローを接続してから各段階を実装しました。
この分割には、責務を分けて段階ごとの検証をしやすくする意図がありました。
LLM を用いた開発でも、対象の処理とその入出力に関係する情報へ絞ることで、
他の段階の詳細が作業に入り込むノイズを減らせると考え、Interaction Flow の考え方をジェネレーター自身の設計に適用しました。

`0c2acdf` では、テンプレートの解析、型の束縛、同期・非同期の展開、手書きメンバーとの照合を通し、
`GeneratorDriver` で生成コードのコンパイルと、無関係な編集後の増分処理の再利用を検証しました。
当初は対象の宣言にコールバック用プロパティの名前を列挙する設計でしたが、
`2c0aaca` で Result のプロパティに付けた属性から成功・失敗の区分と順序を決める方式に変えました。
`Result<T, TError>` の失敗では、この順序に沿って例外と失敗値をコールバックへ渡します。
続いて同じ型への連結では既存の結果をそのまま引き継げるようにし、生成先で同名の型が競合する場合の型名の修飾も検証しました。
[ジェネレーターの入口](../InteractionFlow.ResultAPIGenerator/ResultApiGenerator.cs)、
[ジェネレーターのテスト](../InteractionFlow.Analyzers.Tests/ResultApiGeneratorTests.cs)

### 非同期の受け手とコールバック

17日の `b18a794` では、待機可能型と `ConfigureAwait(false)` の利用可否を型として検証する仕組みを追加しました。
`0590b33` では構成済み awaitable を受け手に取れるようにし、
`416ff74` で非同期の受け手を1回だけ直接 `await` する形に切り替えました。
通常の `Task` / `ValueTask` は通常の `await` の規則で再開し、
構成済みの受け手を渡した場合はその待機点に呼び出し側の指定が適用されます。
生成コードは、呼び出した非同期コールバックの戻り値を `ConfigureAwait(false)` で待てる場合、その形で待機します。
コールバックを呼び出す位置は受け手の待機後で、コールバック内部の待機はその実装自身の指定に従います。

## 利用側の移行

公開シグネチャを新しい型に書き換える前提で、基準コミットの C# ソースから旧型と旧拡張の利用候補を拾いました。
候補は 95 ファイルに及び、Core の Port から Standard、サンプル、テストダブル、ソリューション外のベンチマークへと依存が広がっていました。
このため、Port とその実装・利用側を同じ段階で切り替え、段階ごとにビルドとテストを通しました。

成功値の変換も次の Result を返す処理も `Then` にそろえ、同期 Result から `ThenAsync` や `ThenValueAsync` に直接つなげました。
非同期の受け手には同期コールバックも渡せます。変換した値は `AsResult()` または `CreateSuccess` で成功結果にし、
同期処理を `Task` のまま公開する箇所は `Task.FromResult` で完了済みの結果を返しました。

| コミット | 作業と判断 |
| --- | --- |
| `098166c` | 新 API を `InteractionFlow.Core.ResultAPI.Entities` に配置しました。初回の切り替えで検討した Analyzer の名前空間特例は取り消し、既存の Entities の依存規則で参照できる配置にしました。 |
| `d58c24f` | `ReactionEnd` / `InteractionEnd`、キャンセル待機・タイムアウト、`Entry` / `Storage` を切り替えました。終了結果とキャンセル処理を直接使う Raylib、EscapeGame、Parrot、テスト、ベンチマークもこの段階で追従させました。 |
| `93cef49` | 永続化・シリアライズの Port、`PersistentEntry`、Standard の実装、Notepad の直接の利用箇所をまとめて切り替えました。`TextSerializer` では同期 Result から文字列変換へ接続し、後続にストリームと文字列を渡す処理を `ThenAsync` にそろえました。 |
| `83886c6` | Standard Console の環境取得と、Notepad の選択・編集・ログイン・Secure セッションを切り替えました。Raylib と EscapeGame はテストと実画面で改めて確認しました。 |
| `2d5c26b` | 旧 Result 型と拡張メソッドを削除し、新旧の同名型を区別していた `using` の別名宣言を整理しました。旧 `Map` のテストは `Then` で成功値の変換と失敗伝播を検証する形に移しました。 |
| `fe8a410` | `ResultException` も Result と同じディレクトリと名前空間へ移し、利用側とジェネレーターのテスト設定を更新しました。 |

旧 `Try` の移行では、成功判定に `IsSuccess`、成功値の取得に `HasValue`、例外の取得に `HasError` を使いました。
成功時の副作用は `OnSuccess` から `IfSuccess` に切り替えました。
値付き Result の `default` を検査すると、新 API は `ResultException` を送出します。
`CancellationScheduler` は未初期化の結果も明示的な失敗 Result に変換し、対象処理の例外・キャンセルを結果に変換する既存の境界を維持しました。

### 復帰指定の見直し

利用側では、次のコールバックに入る前の待機で復帰を省く場合に `.ConfigureAwait(false).ThenAsync(...)` などで指定できるようになりました。
チェーン途中と末尾の指定を見直し、ストリームの読み書きやファイルの破棄にあった明示指定は維持しました。
独自の `SynchronizationContext` や `TaskScheduler` を持たないコンソール実行では、復帰先への追加のキュー投入は想定していません。
UI コンテキストから呼ぶ場合、未完了 Task を待った後の変換や一括更新が UI 側に戻る可能性があります。
今回の検証では動作と所有スレッドを確認しました。復帰指定ごとの性能差は未計測です。

## 結果と検証

名前空間を確定した後の切り替えは、既存の責務分割と処理順序を保ったまま、型と拡張メソッドの置き換えを中心に完了しました。
たとえば `TextSerializer` では同期 Result から直接 `ThenAsync` で変換・書き込みへ進み、
`PersistentEntry` では読み込み成功時の値の更新を `IfSuccessAsync` で表せるようになりました。
このように、処理順序や成功時に行うことをコードに素直に表しながら、既存の目的動作を実現できました。

`Then`、`ThenError`、`Resolve`、`IfSuccess`、`IfError`、`IfElse` の **6つのテンプレート関数**で、
現行 API の **286件のオーバーロード**を生成・管理しています。
内訳は同期版26件、`Task` 版130件、`ValueTask` 版130件です。
同期版の拡張クラスには、これに加えて手書きの `AsResult` が1件あります。
[テンプレート関数の定義](../InteractionFlow.Core/ResultAPI/Entities/ResultTemplate.cs)、
[生成後のメソッド数の検証](../InteractionFlow.Analyzers.Tests/ResultApiGeneratorTests.cs#L75)

`Result`、`Result<T>`、`Result<T, TError>`、`ResultException` は
`InteractionFlow.Core.ResultAPI.Entities` にそろいました。
失敗 Result の伝播、送出例外、Task のキャンセルはそれぞれの境界で扱い、
終了処理や一時ファイルの後始末に関する既存のテストも引き継ぎました。

- 最終コミット相当のソリューション全体のビルドは警告・エラーともに 0 件で、645 件のテストが通過しました。
- HelloDoor の開閉、Parrot の会話と前回モードの再実行、Notepad の作成・一覧表示、Secure 版の認証失敗と再ログイン後の読み戻しを確認しました。
- Raylib と EscapeGame の 204 テストを確認し、実画面でカウント更新、ターン進行、両方の Exit によるウィンドウ終了を確認しました。
- 旧 API を削除した段階で、ソリューション外のベンチマークをビルドしました。ローカル NuGet を別プロジェクトへインストールし、生成済みの新 API の利用と旧 Result 型の不在を確認しました。
