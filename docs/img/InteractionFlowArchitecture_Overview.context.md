# Interaction Flow Architecture - Overview Context

このドキュメントは、`docs/img/src/InteractionFlowArchitecture_Overview.drawio` の図が表している意味を、図を直接参照できない状況でも利用できるように言語化したコンテキストである。

対象図は **Interaction Flow Architecture - Overview**。バージョンは **version 3.7 / 2026.07.25** である。図の主題は、Interaction Flow Architecture の静的な全体構造であり、`User`、`Context`、`System`、およびその内部にある `Layers` と `Blocks` の関係を示している。

## 全体像

このアーキテクチャでは、相互作用は `User` と `System` の間で継続する体験として扱われる。

`Context` は、現在の `SystemFlow` に関する状態、状況、文脈的情報を表す。最初に与えられた `Context` を元に `SystemFlow` が実行される過程で `Context` は更新される。更新された `Context` を再利用することで、相互作用を含む連続した体験を実現する。

## 主要な構成要素

### User

`User` は `System` と相互作用する主体である。

図では、人間、ロボット、AI エージェント、その他の自動化されたエージェントや他システムも `User` に含まれることが示されている。つまり、ユーザーは必ずしも人間に限定されない。

### Context

`Context` は、現在の `SystemFlow` に関する状態や状況を表す文脈的情報である。

単なる入力値ではなく、`SystemFlow` の実行中に参照・更新され、その後の相互作用へ引き継がれる状態として描かれている。`Context` が更新され再利用されることで、連続した Interaction が成立する。

### System

`System` は、Interaction Flow Architecture の中心的な境界である。

内部には `Layers` と `Blocks` があり、`Layers` は実行責務の積み重ねを、`Blocks` はレイヤーと並列して存在する構成要素や依存領域を表す。

## Layers

`Layers` は縦方向に配置されている。図には「各レイヤーは、スコープを持った一時的な `Context` を利用して次の層の動作を変更できる」と示されている。

### SystemFlow Layer

`SystemFlow Layer` は Interaction のオーケストレーターである。システムとユーザーの関係を構築する責務を持つ。

### Interaction Layer

`Interaction Layer` は Function Port のオーケストレーターである。システム内部の目的を達成する責務を持つ。

### Function Port Layer

`Function Port Layer` は Function のインターフェースであり、外部機能への依存を抽象化する。

### Function External Layer

`Function External Layer` は外部依存の機能実装である。機能のための実際の処理を行う。

## Blocks

`Blocks` は `Layers` と並列して存在する構成要素として描かれている。

### SystemFlow Builder Block

`SystemFlow Builder Block` は DI コンテナのラッパーであり、`SystemFlow` の依存オブジェクトを注入する。

### Domain Block

`Domain Block` は、外部に依存しないデータ構造、動作、`System / Application / Service` における前提を定義する。

外部ライブラリ、フレームワーク、OS、DB、File System、外部イベントなどに依存しない領域として位置づけられている。

### External Block

`External Block` は、外部ライブラリ、フレームワーク、OS など、機能実現のための環境を提供する。

図では、`External Block` の内部または周辺に、外部イベント、外部プロパティ、`DB / File System`、`Input / Output` が配置されている。

## 外部要素

`External Block` に関わる外部要素として、次のものが示されている。

- `外部イベント / 外部プロパティ`
- `DB / File System`
- `Input / Output`

これらは Domain の内部前提ではなく、外部依存として扱われる。

## 設計上の意味

この Overview 図は、Interaction Flow Architecture を次の構造として表している。

- `User` は人間だけでなく、ロボット、AI エージェント、他システムも含む相互作用主体である
- `Context` は現在の `SystemFlow` に関する状態・状況・文脈情報である
- `SystemFlow` は `Context` を元に実行され、実行中に `Context` を更新する
- 更新された `Context` を再利用することで、連続した Interaction が実現される
- `Layers` はシステムとユーザーの関係の構築から具体的な機能実装までを段階的に扱う
- `Blocks` は SystemFlow 構築、Domain 前提、External 依存を分離して表す
- `Domain Block` と `External Block` を分けることで、外部依存しない前提と外部機能実装を区別する

要約すると、この図は **Interaction Flow Architecture の構造的な見取り図** であり、`Context` を中心に、ユーザーとの相互作用、レイヤー構造、ドメイン前提、外部依存の分担を表している。

## 推定事項

この draw.io XML には明示的な edge 接続が含まれていない。したがって、要素間の関係は主に包含関係、配置、ラベル、視覚的なグループ構造から読み取っている。

詳細な実行フローや `Context` 更新ループの動きは、`InteractionFlowArchitecture_FlowDiagram.context.md` が表している。
