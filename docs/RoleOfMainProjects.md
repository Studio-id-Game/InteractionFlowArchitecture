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
  - .Core の破壊的変更で .Standard / .Samples を変更してもよい。
## `.Standard`
  - **責務：現実のユースケースで扱いやすい形に整形し、安定したAPIとして提供する**
  - セマンティックバージョニング
  - .Standard の破壊的変更で .Samples を変更してもよい。
## `.Samples`
  - **責務：.Standard の使い方を具体例として示しつつ、APIの過不足や使い方を探索・検証する**
  - バージョン管理しない
  - .Samples で得られた知見をもとに .Standard を実用目線で更新する。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./PHILOSOPHY.md) |
[計算モデルとして](./COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./LIBRARY_IMPLEMENTATION_DETAIL.md) |
