# コードサマリー・説明文書・開発文書の修正プラン

調査基準は `02a70f8` と調査開始時点の作業ツリー。EscapeGame の既存差分と未追跡の Result チェーン計画を含む。以下の修正は未着手。

## 調査範囲

Markdown 63 ファイルを棚卸しし、ローカルリンクとコードフェンスを走査した。C# の XML ドキュメントコメントは 112 ファイル、556 ブロックを XML として検査し、契約の記述を実装・既存テストと突き合わせた。掲載コードは現行ソースとの静的比較を行った。掲載コードのコンパイル、公開 NuGet の実体確認、実端末・実画面の再現は今回未実施。修正に対応する検証を下記に示す。

| 範囲 | 照合先と結果 |
| --- | --- |
| ルート README、NuGet README、各プロジェクト README、Raylib の分析 | プロジェクト設定、登録、公開署名、操作・実行入口。A1、A2、C1、C2、C4 |
| Philosophy、計算モデル、実装概要・詳細、図のコンテキストと画像一覧 | 用語の主語、型との対応、保証の範囲、ローカル参照。A1、A2、B1、B2、B5 |
| Result、Context、Cancellation、Builder、Console、FileSystem、Serialization の説明 | 公開契約、派生用の拡張点、失敗経路、テスト。A3、A4、B1〜B5 |
| Analyzer とテストの README、Package Install Check、手動テスト、計測、リリース、AGENTS、プロセス | 診断設定、ソリューション収録、スクリプト、CI、計測時点、手順の参照。B5、C1〜C3、C5 |
| 設計ガイド、仕様案、設計ノート、アーカイブ | 一般原則とサンプル固有の判断、採否、履歴、作業ツリーの実装。B1、B2、C2、C4、C5 |
| XML ドキュメントコメント | Core、Standard、Analyzer、サンプル、テストを含む。修正候補は B1、B3、B4 |

## A. 掲載コードとリンクを直す

| ID | 種別・指摘と根拠 | 修正内容 |
| --- | --- | --- |
| A1 | **旧情報。** [README の HelloDoor](../../README.md#hello-door-) は旧名前空間、`ReadCommandAsync` / `ReactAsync`、`ConsoleBuilder.Profile`、`Task<ReactionEnd>` の override を掲載している。現行の [Port](../../InteractionFlow.Samples.HelloDoor/ExternalPorts/IDoorOperation.cs)、[Reaction](../../InteractionFlow.Samples.HelloDoor/Externals/ConsoleDoorReaction.cs)、[Interaction](../../InteractionFlow.Samples.HelloDoor/Interactions/OperateDoor.cs)、[Program](../../InteractionFlow.Samples.HelloDoor/Program.cs) と異なる。掲載例は `DoorRules` と `DoorState.ApplyCommand` への分離や必須値の取得も反映していない。[実装概要の HelloDoor](../LIBRARY_IMPLEMENTATION.md#hello-door-flow) のソースリンクは旧ディレクトリを指すものが4件ある。 | README の C# ブロックと前後の説明を実ファイルと揃える。4リンクを現行の Port / External に向ける。欠落した `DoorState` が `RequiredContextValueNotFoundException` を保持する終了結果になる経路と、通常の入出力例を確認する。 |
| A2 | **旧情報。** [実装詳細の Builder 例](../LIBRARY_IMPLEMENTATION_DETAIL.md#system-flow-builder) は `ConsoleBuilder.Profile` を、[Interaction 例](../LIBRARY_IMPLEMENTATION_DETAIL.md#interaction-systemflow) は旧 `Task<ReactionEnd>` の override を使う。 | `ConsoleProfile.Default` と `ValueTask<ReactionEnd>` を使う。`Interaction.ExecuteAsync` と `SystemFlow.ExecuteCoreAsync` の `Task<InteractionEnd>`、`OnCancellation` の `Task` はそれぞれ明示する。型定義を省いた例は断片であることと補完に必要な前提を記載する。 |
| A3 | **誤り。** [永続化のバッチ例](../LIBRARY_IMPLEMENTATION_DETAIL.md#storage-persistence) は `GetOrCreate` と `LoadAll` の送出例外を一括で `Result` 化しており、[Result の境界](../RESULT.md#result-と例外の境界) と食い違う。[ResultBoundaryTests](../../InteractionFlow.Standard.Tests/Entities/ResultBoundaryTests.cs) は委譲先の例外伝播を検証している。 | バッチ例の包括 catch を外し、返された失敗と例外・キャンセルの伝播を別々に示す。既存のキャッシュ登録副作用と値更新条件の説明を使う。 |
| A4 | **曖昧。** [FileSystem の DirectoryPersistence 節](../FILESYSTEM.md#directorypersistence) は「各基底」と断ったうえで `GetDirectoryPath` と `GetFilePath` を並べているが、各メソッドの所属を節内で明示していない。[DirectoryPersistence](../../InteractionFlow.Standard/FileSystem/Externals/DirectoryPersistence.cs) と [FilePersistence](../../InteractionFlow.Standard/FileSystem/Externals/FilePersistence.cs) はそれぞれ対応する片方を提供する。 | 各クラスの節で、その基底クラスから利用できる protected ヘルパーを示す。移行表でも対応先のクラスを明記する。 |

## B. API 契約と設計原則を揃える

| ID | 種別・指摘と根拠 | 修正内容 |
| --- | --- | --- |
| B1 | **旧情報・曖昧。** [実装詳細の Context](../LIBRARY_IMPLEMENTATION_DETAIL.md#context) と[アプリ設計ガイド](../guides/INTERACTIONFLOW_APPLICATION_DESIGN.md#専用contextの契約を型で表現する) と [Context の API 文書の冒頭例](../CONTEXT_VALUES.md#flowcontext-の構成と寿命) は親ありの構成をキャンセル共有と一般化している。[FlowContext](../../InteractionFlow.Core/Entities/Contexts/FlowContext.cs) は値探索とキャンセル共有を独立指定できる。実装詳細は Entry 解決を再帰的とし、破棄時の値の責務を Context 作成側に一律に置く。`With` の XML サマリーも「同じ型」と書くが探索は代入可能性で判定する。 | [Context の API 文書](../CONTEXT_VALUES.md) の継承表に揃え、各例で使用したコンストラクターの挙動と `ContextInheritance` の選択を明記する。`Values` のみで型探索から親のキャンセル値を得る可能性と `context.Cancellation` の違いを例示する。Entry の反復解決、最大100段、上限到達時の循環診断、`ResultException` の経路を説明する。Context の作成・破棄と、追加値の実際の所有者を明示する。XML の `With` は要求型に適合する新しい追加値が優先される旨にする。 |
| B2 | **曖昧。** [README の基本概念と HelloDoor の Tips](../../README.md#vision) は、共有される現在とコード内の状態を結び付ける説明が薄く、HelloDoor の手法を「すべての Context 更新」の説明に広げている。[Philosophy](../PHILOSOPHY.md) と [計算モデル](../COMPUTATIONAL_MODEL.md) は、共有される現在、関係の歴史、実行トレース、System 側の投影を区別している。 | README と[実装概要の更新原則](../LIBRARY_IMPLEMENTATION.md#context-update-principle) で、概念上の Context、Context Loop、System 側の `IFlowContext`、ユーザーの観測をそれぞれ明示する。HelloDoor は状態更新と出力の対応を説明し、実際の観測や両処理の一体性は契約の表現に含めない。計算モデルの条件付き決定性は維持する。 |
| B3 | **旧情報・曖昧。** [キャンセルの説明](../CANCELLATION.md#interaction-との連携) と [実装詳細](../LIBRARY_IMPLEMENTATION_DETAIL.md#interaction-systemflow) は本体が返す `Task` のキャンセルと書く。[Interaction<TContext>](../../InteractionFlow.Core/Interactions/Interaction.Generic.cs) は返された `ValueTask` の消費で生じた `OperationCanceledException` を契機にフックを呼ぶ。XML の `ExecuteAsync` は、Port が値を返しても後始末の失敗が失われる場合に基底が集約例外を送出する経路の記載がない。 | 開始前、本体から同期送出、返却後のキャンセル例外、失敗 `ReactionEnd` の返却を区別する。標準 `CancellationHandling` による待機・リセットと、任意の `ICancellationPort` の責務を分ける。後始末失敗の保持・集約は詳細と XML を整合させ、既存のキャンセル回帰テストを確認する。 |
| B4 | **曖昧。** [PersistentEntryExtensions.LoadAll の XML](../../InteractionFlow.Core/ExternalPorts/PersistentEntryExtensions.cs) は `oldValue` が要素単位で null を許す配列であることを述べるが、参照先の汎用 `Load` 契約と、このメソッドが受け取る配列用 Port の型引数との関係が読み取りにくい。[Storage](../../InteractionFlow.Core/Externals/Storage.cs) と [IStoragePort](../../InteractionFlow.Core/ExternalPorts/IStoragePort.cs) の XML は、一括削除の検査失敗時に登録が残ることを明示していない。[実装詳細](../LIBRARY_IMPLEMENTATION_DETAIL.md#functions) はこの違いを記述済み。 | `LoadAll` では配列の要素が null になり得ることと、配列そのものを成功値として渡すことを明記する。一括削除は検査失敗時に登録を維持し、破棄段階では全件の破棄を試みた後に登録を解除し、失敗を集約して送出する旨を XML に補う。単件削除は登録解除後に値を破棄する順序を示す。 |
| B5 | **曖昧。** [実装概要の保証表](../LIBRARY_IMPLEMENTATION.md#guarantees) の「Analyzer が保証する」は、既定では無効という条件と診断対象の限界が表単体で読めない。[実装詳細の依存ツリー](../LIBRARY_IMPLEMENTATION_DETAIL.md#runtime-dependency-tree) の「実際の出力」も、[HelloDoor の Program](../../InteractionFlow.Samples.HelloDoor/Program.cs) が通常実行で表示するように読める。 | Analyzer の有効化、診断対象、重大度、抑制に関して [Analyzer README](../../InteractionFlow.Analyzers/README.md) を参照する。実行時ツリーは `DependencyTreeView.GetDependencyTreeText(flow.Root)` を呼ぶ場合の出力例として示す。 |

## C. サンプル案内と開発資料を更新する

| ID | 種別・指摘と根拠 | 修正内容 |
| --- | --- | --- |
| C1 | **誤り・曖昧。** [NuGet README](../../NUGET_README.md#はじめる) の Notepad.Secure は Interaction も差し替えると説明するが、[通常版](../../InteractionFlow.Samples.Notepad/Program.cs) と [Secure 版](../../InteractionFlow.Samples.Notepad.Secure/Program.cs) は共通 Profile と MainLoop を用いる。[ルートの開発要件](../../README.md#samples) は SDK と Visual Studio の両方を要件のように書き、次行で CLI 利用には不要としている。 | NuGet README はログイン Operation・保存関連実装の差し替えに修正する。開発要件は `global.json` の SDK、CLI 手順、IDE を利用する場合の条件を明確にする。パッケージの TFM、リポジトリの SDK、公開済みバージョンを別々に確認する。 |
| C2 | **曖昧・案内漏れ。** [描画ガイド](../guides/RENDERING_LIBRARY_INTEGRATION.md#8-起動失敗と終了処理を最初から扱う) の Esc による終了要求は Counter の設定に依存する。EscapeGame は [View の設定](../../InteractionFlow.Samples.EscapeGame/Aspects/View/Externals/RaylibWindowReaction.cs) で自動終了キーを無効にし、[入力取得](../../InteractionFlow.Samples.EscapeGame/Aspects/View/Externals/RaylibWindowOperation.cs) で Esc を選択取消に使う。[ルートのサンプル表](../../README.md#examples) は起動手順に載る Raylib Counter を省いている。 | OS の閉じる要求とサンプル別のキー割り当てを明記する。サンプル表に Counter を加え、フレーム駆動と非同期フローの最小の描画例として EscapeGame との読み順を示す。 |
| C3 | **誤り。** [AGENTS](../../AGENTS.md) は存在しない「トラブルシュート」節への参照を持つ。[プロセス一覧](../processes/README.md) にも該当節はない。 | 既存の SDK・ビルド・テストの確認手順に参照を結ぶか、冒頭の参照を実在する見出しに直す。追加の操作条件を設けず、本文中の見出し参照も検査対象にする。 |
| C4 | **旧情報・曖昧。** [EscapeGame README](../../InteractionFlow.Samples.EscapeGame/README.md#systemflowとポーズ) は作業ツリーの Result チェーンに言及しながら、[旧 Result 調査](../../archives/escape-game/ESCAPE_GAME_RESULT_API_REVIEW.md) を詳細の入口にしている。旧文書は冒頭に後続へのリンクを持つが、節名「最終判断」と当時の結論が目立つ。[後続計画](../../archives/escape-game/ESCAPE_GAME_RESULT_CHAIN_PLAN.md) の記録と [現行フロー](../../InteractionFlow.Samples.EscapeGame/SystemFlows/WindowSystemFlow.cs) では、SystemFlow と Interaction の採否が異なる。同じ README の Shared の表には旧配置の Port 契約が残るが、[名前空間と配置](./ESCAPE_GAME_NAMESPACE_LAYOUT.md) と現行の構成では機能 Port は各担当 Aspect にある。 | README から採用内容へ直接リンクし、Shared の表と各 Aspect の Port 配置を揃える。旧調査と後続計画の各判断に時点・採否を付け、旧案の判断理由を保持する。`WindowSystemFlow` の停止・合流・Close と `GamePlaySystemFlow` の EndSession・リトライ条件を現行ソースと照合する。仕様案は初版の設計意図を記録する資料として、現行の配置・検証結果への入口を設ける。 |
| C5 | **曖昧。** [EscapeGame のテスト点検](./ESCAPE_GAME_TEST_COVERAGE.md) の135件／605件と [README の検証節](../../InteractionFlow.Samples.EscapeGame/README.md#検証) の154件／624件は別の作業時点の記録。README の「今回」や点検の「現在」から時点を判別しにくい。[性能計測と改善計画](./INTERACTION_EXECUTION_PERFORMANCE_PLAN.md) も旧 Task 本体の結果、採用済みの部分実装、後続候補を順に掲載している。 | README は確認できる振る舞いを中心にし、件数と計測値には対象時点・条件を添える。点検の件数を当時の値として明記する。計測文書の入口から採用済みの本体 `ValueTask` 化と後続候補へそれぞれ案内する。実機確認と代替 Port での検証は、該当時点と実行条件を明示する。 |

## 実施順と完了条件

1. A1〜A4 を修正する。HelloDoor の掲載単位を対応する実ファイルと比較し、掲載コードだけで組み立てた一時プロジェクトをビルドする。Builder、Echo、永続化の API 例は省略した型と using を補って組み立て、旧署名と現在の署名を混ぜていないことを確認する。歴史上の例や擬似コードは対象時点・前提を表示する。
2. B1〜B5 を修正する。`EntryDepthTests`、`FlowContextTests`、`InteractionCancellationTests`、`TypedInteractionTests`、`FlowResultTests`、Analyzer テストと説明を照合する。ビルド時の XML ドキュメント診断と生成 XML を確認する。Storage は削除前検査と破棄の順序、永続化例は返却失敗と送出例外の経路を確認する。
3. C1〜C5 を修正する。EscapeGame の関連差分が確定した時点のソースと記録に合わせる。公開パッケージ向けの主張は対象版の実体に照合し、必要な場合は `Package Install Check` の検証手順を用いる。図の意味を変更する箇所が確定した場合は、図の更新手順に沿って関連成果物を同期する。
4. ローカルのファイルリンク・アンカー、外部公開リンク、文書と XML の差分を再点検する。`dotnet build InteractionFlow.slnx` と `dotnet test InteractionFlow.slnx` をリポジトリの手順で実施する。サンプルの操作説明に手を入れた箇所は、記載の環境で起動し、実行した確認の範囲を記録する。履歴の値と現行契約の双方を保てていることを差分で確認する。

## セルフレビューの結果

1. 根拠となる宣言と実装本体を再読した。`TextSerializer<T>` は文字列用と Stream 用の両方の Port を実装するため、当初の指摘から外した。バッチ例の例外境界は既存テストを根拠に残した。`LoadAll` の XML はリンク切れの断定を取り下げ、配列を渡す契約の説明に絞った。
2. 周辺節と履歴を再照合した。`FlowContext(parent)` の既定動作と継承を指定した場合、初版の設計と作業ツリーの採用結果、記録時点のテスト件数と現在の検証範囲を区別した。図の3件は `.drawio` と `.context.md` の版・日付が一致したため、図の更新を作業項目に加えなかった。
3. 修正項目ごとに対象・実装根拠・実施順・検証方法を確認した。公開パッケージや実機での動作は、修正後に確認する事項として明記した。
