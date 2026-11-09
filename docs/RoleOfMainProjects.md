<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./PHILOSOPHY.md) |
[計算モデルとして](./COMPUTATIONAL_MODEL.md) |
[ライブラリ実装](./LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./LIBRARY_IMPLEMENTATION_DETAIL.md) |

---

# .Core/.Standard/.Samples それぞれの役割 <a id="main-project-roles"></a>

本ドキュメントでは、中心となるC#プロジェクト（.Core/.Standard/.Samples）の役割について説明します。

## `.Core`
  - **責務：アーキテクチャ概念を、構造と振る舞いとして副作用や利用形態に依存せずに定義する**
  - セマンティックバージョニング
  - アーキテクチャ概念の変更に伴い更新する。
  - CancellationObject と CancellationScheduler は、キャンセル状態・世代・通知の基盤契約を同じ層で管理する。
  - .Core の破壊的変更で .Standard / .Samples を変更してもよい。
## `.Standard`
  - **責務：現実のユースケースで扱いやすい形に整形し、安定したAPIとして提供する**
  - セマンティックバージョニング
  - .Standard の破壊的変更で .Samples を変更してもよい。
## `.Samples`
  - **責務：.Standard の使い方を具体例として示しつつ、APIの過不足や使い方を探索・検証する**
  - バージョン管理しない
  - .Samples で得られた知見をもとに .Standard を実用目線で更新する。

`InteractionFlow.Samples.Raylib` は、フレーム単位の入力・描画と非同期フローの接続例です。
`Program` は構成と実行開始に絞り、`WindowSystemFlow` が画面の起動・フレーム進行・終了を調整します。
入力は Operation、ウィンドウの生成・描画・終了と結果通知は Reaction に分けます。
`CounterFlowContext` が用途別のアクセス契約を提供し、分割したゲーム状態・表示・入力通知を共通の同期管理で扱います。
型付きの SystemFlow・Interaction・Port が同じ Context 型を引き継ぎ、確認付きプロパティからアクセスします。
Context 固有の追加値検証によって、固定されたアクセス契約の隠蔽を拒否します。
カウンターの生の可変状態は提供せず、ウィンドウ状態の `WindowState` はフレーム用 Context に置きます。
フレームとカウンターは別の CancellationObject で並行実行し、
raylib の呼び出しはメインスレッドに保ちます。
構成と実行方法は [Raylib counter](../InteractionFlow.Samples.Raylib/README.md) を参照してください。

`InteractionFlow.Samples.EscapeGame` は、その構成を小さなターン制脱出ゲームへ適用します。
関心別Stateの整合した更新、SystemFlowによるターン合成と実行寿命管理、提示完了待機、
プレイ中のポーズ・再開、リトライと終了処理を検証します。
AccessをGameplay・View・Lifetimeの3契約とし、Contextの引数契約も役割別に分けています。
EscapeApplicationContextが共有EscapeAccessを作り、GameplayContext / GameViewContextは公開Accessと探索範囲を限定します。
Cancellationの共有と親の値探索はContextInheritanceで独立に指定します。
構成と操作は [Turn-based escape](../InteractionFlow.Samples.EscapeGame/README.md) を参照してください。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./PHILOSOPHY.md) |
[計算モデルとして](./COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./LIBRARY_IMPLEMENTATION_DETAIL.md) |
