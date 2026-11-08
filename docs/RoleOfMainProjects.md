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
カウント・表示・入力受付・終了意図を内部で同期する `CounterState` と、ウィンドウ状態の `WindowState` を Context に渡します。
フレームとカウンターは別の CancellationObject で並行実行し、
raylib の呼び出しはメインスレッドに保ちます。
構成と実行方法は [Raylib counter](../InteractionFlow.Samples.Raylib/README.md) を参照してください。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./PHILOSOPHY.md) |
[計算モデルとして](./COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./LIBRARY_IMPLEMENTATION_DETAIL.md) |
