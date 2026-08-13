<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../../README.md) |
[Philosophy](./../PHILOSOPHY.md) |
[計算モデルとして](./../COMPUTATIONAL_MODEL.md) |
[ライブラリ実装](./../LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../LIBRARY_IMPLEMENTATION_DETAIL.md) |

---

# Interaction Flow Architecture - Flow Diagram Context

このドキュメントは、`docs/img/src/InteractionFlowArchitecture_FlowDiagram.drawio` の図が表している意味を、図を直接参照できない状況でも利用できるように言語化したコンテキストである。

対象図は **Interaction Flow Architecture - Flow Diagram**。バージョンは **version 3.8 / 2026.07.26** である。図の主題は、`Program`、`User`、`SystemFlow`、Layer 群、外部機能、そして `Context` 更新ループがどのように連動するかである。

## 全体像

この図は、`Context` のライフサイクルと `SystemFlow` の実行経路を中心にした Interaction 設計図である。

`Program` は `Context` を作成または再利用し、`SystemFlow` を構築して実行し、更新された `Context` を次回以降へ引き継ぐ。`User` は Operation と Reaction を通じてシステムと相互作用する。`System` は Layer と Block を通じて `SystemFlow` を実行し、外部依存は External 側で扱う。

この図の中心的な意味は、**相互作用を `SystemFlow` として実行し、その過程で `Context` を更新し、更新後の `Context` を次の相互作用に再利用する** ことである。

## 主要な構成要素

### Program

`Program` は実行の起点である。

エントリーポイント、イベント、リクエストを受け取り、`Context` を作成または再利用し、`SystemFlow` を実行する。また、`Context` 更新ループを維持する役割を持つ。

### User

`User` は相互作用を進める主体である。

図では、人間、ロボット、AI エージェントなどが含まれている。`User` は入力として `Operation` を行い、出力として `Reaction` を受け取り、その相互作用を通じて `Context` 更新ループを進める。

### System

`System` は、Layer 群と Block 群を含む中心領域である。

ここで `SystemFlow` が実行され、ユーザーの目的、システムの目的、機能の目的が段階的に実現される。

### Context 状態

図には次の `Context` 状態が示されている。

- `New Context`: 新規作成または準備された Context
- `Current Context`: 現在の `SystemFlow` で利用される Context
- `Updated Context`: 実行と相互作用の結果として更新された Context
- `Next Context`: 次回以降の実行に再利用される Context

これらが循環することで、継続的な `Context` 更新ループが形成される。

## Layers / Blocks

### SystemFlow Builder Block

`SystemFlow Builder Block` は DI コンテナのラッパーであり、`SystemFlow` の依存オブジェクトを注入する。

### Domain Block

`Domain Block` は、外部に依存しないデータ構造と動作、および `System / Application / Service` における前提を定義する。

### Layers

Layer 群には次の要素がある。

- `SystemFlow Layer`: Interaction のオーケストレーター。システムとユーザーの関係を構築する
- `Interaction Layer`: Function Port のオーケストレーター。システム内部の目的を達成する
- `Function Port Layer`: Function のインターフェース。外部機能への依存を抽象化する
- `Function External Layer`: 外部依存の機能実装。機能のための実際の処理を行う

図には、各層がスコープを持った一時的な `Context` を利用して、次の層の動作を変更できることが示されている。

### External Block

`External Block` は、外部ライブラリ、フレームワーク、OS など、機能実現のための環境を提供する。

`Function External Layer` が実際の処理を行う際の外部依存領域として位置づけられている。

## 色と矢印の意味

図では、矢印の色によってフローの意味が分けられている。

- オレンジ: `C1` - `C3` の `Context Loop`
- 青: `P1` - `P6` の `SystemFlow` 実行
- 赤: `U1` - `U2` の `User` 相互作用
- 緑: `E1` - `E2` の外部機能連携

また、通常矢印は 1 回の動作フローを表し、縞矢印は 1 回以上の動作フローを表す。

## 主要フロー

### C1. SystemFlow での Context の利用

`C1` は `New Context` から `Current Context` へ向かうオレンジのフローである。

新規作成または再利用された `Context` が、現在の `SystemFlow` で利用される状態になることを表す。

### P1. SystemFlow 構築 + 実行

`P1` は `Program` から `SystemFlow Builder Block` を経由して Layer 群へ向かう青いフローである。

`Program` が `SystemFlow` を構築し、Layer 群での実行を開始することを表す。

### P2. ユーザーの目的の実現

`P2` は `SystemFlow Layer` に対応する青い下向きフローである。

Interaction をオーケストレーションし、ユーザーの目的を実現する段階を表す。

### P3. システムの目的の実現

`P3` は `Interaction Layer` に対応する青い下向きフローである。

Function Port をオーケストレーションし、システム内部の目的を達成する段階を表す。

### P4. 機能の目的の実現

`P4` は `Function Port Layer` から `Function External Layer` にかけての青い下向きフローである。

Function の目的を、外部依存を伴う実装によって実現する段階を表す。

### P5. 動作要求

`P5` は外部リソース付近に配置された青い縞矢印である。

Function または External 側から外部依存や外部リソースへ動作を要求する流れを表すと読める。draw.io 上では主に矢印図形として描かれているため、接続先は配置とラベルからの推定である。

### P6. 動作実現

`P6` は外部リソース付近に配置された青い矢印である。

外部依存や外部リソースによって要求された動作が実現される流れを表すと読める。`P5` と同様に、接続関係は明示 edge ではなく視覚的配置からの推定である。

### C2. Context の更新

`C2` は `Current Context` から `Updated Context` へ向かう大きなオレンジの下向きフローである。

`SystemFlow` の実行中、Layer 群が `Context` を参照・更新しながら処理を進めることを表す。

### C3. SystemFlow で更新された Context の再利用

`C3` は `Updated Context` から `Next Context` へ向かうオレンジのフローである。

`SystemFlow` によって更新された `Context` が次回以降に再利用され、`Context` 更新ループが閉じることを表す。

## User との相互作用

赤いフローは `User` との相互作用を表す。

- `U1. 操作 (Operation)`: User からシステムへの入力
- `U2. 反応 (Reaction)`: システムから User への出力

この相互作用は単なる入出力ではなく、`Context` 更新ループを進める要因として描かれている。

## 外部機能連携

緑のフローは外部機能連携を表す。

- `DB / File System` との `E1. 永続化 (Storage)`
- `外部イベント / 外部プロパティ` との `E2. その他状態変化 (Silent External)`

これらは `External Block` 側の外部依存として扱われ、内部の Domain とは分離される。

## 設計上の意味

この Flow Diagram は、Interaction Flow Architecture を実行時のループとして表している。

- `Program` がトリガーを受け取り、`Context` を準備する
- `SystemFlow Builder Block` が `SystemFlow` を構築する
- Layer 群が、ユーザーの目的、システムの目的、機能の目的を段階的に実現する
- `Function Port Layer` が Function のインターフェースとして外部機能への依存を抽象化する
- `Function External Layer` と `External Block` が外部依存を伴う実際の処理を担う
- `User` の `U1. 操作 (Operation)` と `U2. 反応 (Reaction)` が相互作用を進める
- `Context` が更新され、次の相互作用へ再利用される

要約すると、この図は **`SystemFlow` の実行を通じて `Context` を更新し続ける Interaction の実行モデル** を表している。

## 推定事項

draw.io XML には少数の explicit edge が含まれているが、主要なフローの多くは接続 edge ではなく矢印図形として描かれている。そのため、関係の一部は矢印の向き、座標、ラベル、近接関係から推定している。

特に `P5`、`P6`、外部リソース付近のフローは、明示的な source / target 接続ではなく視覚配置からの推定を含む。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../../README.md) |
[Philosophy](./../PHILOSOPHY.md) |
[計算モデルとして](./../COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./../LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../LIBRARY_IMPLEMENTATION_DETAIL.md) |
