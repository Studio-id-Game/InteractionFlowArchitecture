# Interaction Flow Architecture - Dependency Diagram Context

This document captures the meaning of `docs/img/src/InteractionFlowArchitecture_DependencyDiagram.drawio` so the diagram can be used as text context.

## Overview

対象図は **Interaction Flow Architecture - Dependency Diagram**。バージョンは **version 3.6 / 2026.07.14** である。

この図は、Interaction Flow Architecture の主要要素間の依存方向を示す依存関係図である。実行順序そのものではなく、`Program`、`SystemFlow Builder Block`、各 Layer、`Domain Block`、`External Block`、Port、外部実体の間で、どの要素がどの抽象・実装・外部資源に依存するかを表している。

中心的な意味は、`Interaction` が Function Port に依存し、`Function External Layer` 側の実装がその Port を実装することで、外部依存を Port 境界の外側に分離することである。

## Legend

- 破線矢印は依存を表す。
- 白抜き矢印は Port の実装を表す。
- 各レイヤー内での横方向の依存関係は規定しない。
- `Storage` の対象は `DB`、ファイルシステム、環境設定、個人設定などであり、永続・一時を問わない。

## Main Elements

### Program

`Program` はエントリーポイント、イベント、リクエストを受け取る外側の起点である。`Context` の作成または再利用、`SystemFlow` の実行、`Context` 更新ループの維持を担う。

`Program` は `SystemFlow Builder Block` に依存し、ラベル `SystemFlow / Handler の生成` により、`SystemFlow Handler` の生成を行うことが示されている。また、`Program` は `SystemFlow Layer` にも依存し、ラベル `SystemFlowHandler を介して実行` により、生成された Handler を介して `SystemFlow` を実行することが示されている。

### SystemFlow Builder Block

`SystemFlow Builder Block` は DI コンテナのラッパーであり、`SystemFlow` の依存オブジェクトを注入する。

内部には次の要素がある。

- `SystemFlow / Builder`: `SystemFlow` 生成 DI ビルダー。
- `SystemFlow / Handler`: 生成済み DI スコープと生成済み `SystemFlow` を持つ。

`SystemFlow / Builder` から `SystemFlow / Handler` への依存には `生成` ラベルがあり、Builder が Handler を生成することを表す。

`SystemFlow Builder Block` は各 Layer に依存する。`SystemFlow Layer` への依存は `Program の要求で生成`、`Interaction Layer` への依存は `SystemFlow の依存で生成`、`Function Port Layer` への依存は `定義参照`、`Function External Layer` への依存は `実装生成` として示されている。つまり Builder Block は、Program の要求に応じて `SystemFlow` を生成し、`SystemFlow` が依存する `Interaction` を生成し、Port 定義を参照し、外部実装を生成して組み立てる。

### Layers

`Layers` は `System / Application / Service` 内の主要な縦方向構造である。図には「各レイヤーは、スコープを持った一時的な `Context` を利用して次の層の動作を変更できる」と示されている。

- `SystemFlow Layer`: `Interaction` のオーケストレーターであり、システムとユーザーの関係を構築する。
- `Interaction Layer`: `Function Port` のオーケストレーターであり、システム内部の目的を達成する。
- `Function Port Layer`: Function のインターフェース。外部機能への依存を抽象化する。
- `Function External Layer`: 外部依存の機能実装であり、機能のための実際の処理を行う。

`SystemFlow` は `Interaction` に依存する。`Interaction` は `IOperationPort`、`IReactionPort`、`IStoragePort`、`ISilentExternalPort` に依存する。

`SystemFlow` から `Interaction` への依存には、
`Interaction を組み合わせてユーザーへの反応プロセスをフロー化` というラベルがある。

### Domain Block

`Domain Block` は、外部に依存しないデータ構造、動作、`System / Application / Service` における前提を定義する。

各 Layer から `Domain Block` へ依存矢印があり、`SystemFlow Layer`、`Interaction Layer`、`Function Port Layer`、`Function External Layer` がドメイン定義を参照することを表す。

図中には `Entity_A`、`Entity_B` と `概念 A`、`概念 B` があり、`Entity_A` から `Entity_B` への依存に `Domain で閉じた / 内部依存の例` というラベルがある。これは Domain 内部に閉じた依存関係の例である。`Entity_B` から先は `（以下略…）` として省略されている。

### External Block

`External Block` は外部ライブラリ、フレームワーク、OS など、機能実現のための環境を提供する領域である。

外部実体として次のものが示されている。

- `Input`: 入力実体。
- `Output`: 出力実体。
- `DB / File System`: 永続化または一時保存の対象。
- `外部イベント / 外部プロパティ`: ユーザーには直接見えない外部状態やイベント。

## Ports And Implementations

`Function Port Layer` には次の Port がある。

- `IOperationPort`: 仮想入力。
- `IReactionPort`: 仮想出力。
- `IStoragePort`: 仮想永続化。
- `ISilentExternalPort`: 仮想状態変化。

`Function External Layer` には次の実装がある。

- `Operation`: 入力実体を扱う。
- `Reaction`: 出力実体を扱う。
- `Storage`: 永続化実体を扱う。
- `SilentExternal`: 状態変化実体を扱う。

白抜き矢印と具体的な実装ラベルにより、次の Port 実装関係が示されている。

- `Operation` は `IOperationPort` を `入力実装` として実装する。
- `Reaction` は `IReactionPort` を `出力実装` として実装する。
- `Storage` は `IStoragePort` を `永続化実装` として実装する。
- `SilentExternal` は `ISilentExternalPort` を `状態変化実装` として実装する。

通常の破線依存により、各実装は対応する外部実体にも依存する。

- `Operation` は `Input` に依存する。
- `Reaction` は `Output` に依存する。
- `Storage` は `DB / File System` に依存する。
- `SilentExternal` は `外部イベント / 外部プロパティ` に依存する。

## Dependency Meaning

この図では、上位の目的達成単位から下位の抽象へ依存が向かう。

`SystemFlow` は `Interaction` に依存し、`Interaction` は Function Port に依存する。実際の外部処理は `Function External Layer` 側の `Operation`、`Reaction`、`Storage`、`SilentExternal` が実装する。これにより、`Interaction Layer` は外部実体ではなく Port を使い、外部依存の詳細は実装側に閉じ込められる。

`SystemFlow Builder Block` は、この依存構造を DI によって組み立てる責務を持つ。`Program` は Builder Block によって `SystemFlow / Handler` を生成し、その Handler を介して `SystemFlow Layer` を実行する。

## Assumptions And Inferences

この context は draw.io XML の明示 edge、ラベル、親子関係、配置から作成している。Port は矢印形状の swimlane/vertex として描かれており、方向は明示 edge と配置から解釈している。

`Function External Layer` の実装から Port への白抜き矢印は、図の凡例と edge style から Port 実装関係として解釈している。
