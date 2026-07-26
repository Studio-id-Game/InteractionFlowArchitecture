[Interaction Flow Architecture](../README.md)

# ライブラリの実装

## このページの目的 <a id="purpose"></a>

README では、Interaction Flow Architecture を次のような Context Loop として説明しています。

```text
Context → Interaction → next Context → next Interaction → ...
```

このページでは、この流れがライブラリ上でどのように実行されるかを説明します。

中心にするのは、個々の API の網羅的なリファレンスではありません。

- README のアーキテクチャ概念が、どの型に対応するか
- `Program` から `SystemFlow`、`Interaction`、`Function` へ、処理がどう進むか
- `Context` がどこで参照・更新され、次の Interaction へどう引き継がれるか
- ライブラリが型として保証することと、設計原則として期待することの違い
- Builder、Storage、Analyzer が Context Loop の実装をどう支えるか

を、実際のソースコードに沿って整理します。

個別 API の引数、戻り値、例外などの詳細は、各型の XML ドキュメントコメントも参照してください。

## 目次

- [全体像](#overview)
- [Context Loop の実行経路](#execution-path)
- [SystemFlow・Interaction・実行環境](#runtime-components)
- [`IFlowContext` インスタンスと終了結果](#context-and-results)
- [データ保持と永続化](#data-and-persistence)
- [設計上の保証とプロジェクト境界](#architecture-boundaries)
- [現在の制約と改善候補](#future-improvements)

# 全体像 <a id="overview"></a>

この章では、ドキュメントの対象範囲と、アーキテクチャ概念に対応する主な型を整理します。

## アーキテクチャ概念とライブラリの対応 <a id="concept-mapping"></a>

README で定義している概念と、主に対応するライブラリ要素は次のとおりです。
これは概念全体と型の一対一対応ではなく、実装の中心となる型や実行経路です。

| アーキテクチャ概念 | 主なライブラリ/プログラム要素 | 実装上の役割 |
| --- | --- | --- |
| `User` | プログラム利用者 | `System` と関係する存在として、`System Flow`、`Interaction`、`IOperationPort`、`IReactionPort` 等の実装に想定される |
| `System` | プログラム全体 | エントリーポイントを持ち、1つ以上の `SystemFlow` を実行し、一連のユーザー体験を提供する |
| `System Flow` | `ISystemFlow<TContext>` / `SystemFlow<TContext>` | 1つ以上の `Interaction` を実行し、`System` の側から `User` との関係を構築する |
| `Interaction` | `IInteraction` / `Interaction` | 1つ以上の `Function` を実行し、システム内部の目的を一段進める |
| `Function` | `Function Port` / `Function External` | `Function Port` : 機能を契約として定義する<br/>`Function External` : 機能を実装として実現する |
| `Context` | `IFlowContext` / `FlowContext` / `ScopedFlowContext` | `System` が実行する `System Flow` および `Interaction` の中で、経路の決定のために参照したり、次に引き継いだりするための文脈値を提供する |
| `Context Loop` | `System` の実行経路と `User` の行動選択 | 代表的に1つの `IFlowContext` インスタンスが、`System` と `User` の関係の中で更新され続けることで、`User` に提供する体験に連続性を与える |
| `Operation` | `IOperationPort` とその実装 | `User` による操作や入力を受け取る |
| `Reaction` | `IReactionPort` とその実装 | `System` から `User` への観測可能な反応を提供し、その反応と対応する `Context` への影響を表す |
| `Storage` | `IStoragePort` とその実装<br/><small>（実装概念の `Persistence`、`Serializer` も参照）</small> | `Context` の文脈的な意味から独立して、再利用する値をメモリ上に保持する |
| `Silent External` | `ISilentExternalPort` とその実装 | `User` との相互作用やデータの記録を目的とせず、外部環境と連携する |

また、実装においては以下の概念も重要になります。

| 実装概念 | 主なライブラリ/プログラム要素 | 実装上の役割 |
| --- | --- | --- |
| `Program` | Program などのエントリーポイント | `System` としてのエントリーポイント。`System Flow Builder` によって `SystemFlow` を組み立てて、`IFlowContext` を用いて実行する |
| `System Flow Builder` | `SystemFlowBuilder<TContext>` / `SystemFlowHandler<TContext>` など  | `SystemFlow` と依存オブジェクトを実行可能なスコープとして組み立てる |
| `External` | 外部ライブラリなど | データベースやファイルシステム、UI などの外部機能 |
| `Domain` | `Entities` 以下のクラスなど | 外部環境に依存しない `System` の前提を表す、データ構造や計算。`Entities` 以外にも、依存関係のルールが破綻しない範囲で自由に設置できる |
| `Function Port` | `IOperationPort`、`IReactionPort` など | `Interaction` から見える機能の契約。機能を意味論的に表す。 |
| `Function External` | 各 `Function Port` を実装する `Operation`、`Reaction`、`Storage` など | 各 `Function Port` の機能を実際の実行環境へ接続する |
| `Persistence` | `IPersistencePort` とその実装 | 主に `Storage` の補助として、`IPersistencePort` やその派生インターフェースで永続化機能を抽象化し、実装によって実際の実行環境へ接続する |
| `Serializer` | `ISerializerPort` とその実装 | 主に `Storage` の補助として、`ISerializerPort` やその派生インターフェースで保存形式変換を抽象化し、実装によって実際の実行環境へ接続する |

<details>
<summary> 💡 Tips: Storage / Context / Context Loop / 計算モデルとの対応について </summary>

> - **Storage との対応**
>
>   アーキテクチャ概念の `Storage` は、
>   メモリ上の一時データから DB やファイルシステムの永続データまでを含む広い概念です。
>
>   このライブラリでは、`Storage` の概念を、メモリ上の値を所有する `IStoragePort` / `Storage`、
>   永続データを読み書きする `IPersistencePort`、保存形式を変換する `ISerializerPort` へ分割して実装しています。
>
> - **Context / Context Loop との対応**
>
>   このドキュメントでは、アーキテクチャ上の概念を `Context`、
>   実行時に API 間で受け渡されるオブジェクトを `IFlowContext` インスタンスと表記します。
>   また、`Context Loop` は、一つのクラスへ直接対応せず、複数の型による実行経路全体で表現されます。
>
> - **計算モデルとの対応**
>
>   `IFlowContext` が提供するのは `System` が扱う概念としての `Context` の一部であり、計算モデルにおける `ContextTape` と同じ実体ではありません。
>   また、`Function` の分類は、計算モデルでの考察に基づいて、物理的な配置ではなく利用目的で分類されます。
>
> 詳しくは、以下を参照してください。
> - [User と System の間にある ContextTape](./COMPUTATIONAL_MODEL.md#user-と-system-の間にある-contexttape)
> - [システム全体のテープ構成と Function の役割](./COMPUTATIONAL_MODEL.md#システム全体のテープ構成と-function-の役割)

</details>


## 依存関係の全体像

![Interaction Flow Architecture dependency diagram](./img/InteractionFlowArchitecture_DependencyDiagram.svg)

代替テキスト: [Interaction Flow Architecture - Dependency Diagram Context](./img/InteractionFlowArchitecture_DependencyDiagram.context.md)

この図は、SystemFlow、Interaction、Function Port、Function External、
Domain、外部環境、および Builder の「概念上の依存構造」を示します。

このライブラリの構造は大きく分けて、4層の Layer と3つの Block、およびエントリーポイントとしての Program によって構成されます。

| 構造 | 詳細 |
| - | - |
| `Program` | `System` としてのエントリーポイント。`System Flow Builder` によって `SystemFlow` を組み立てて、`IFlowContext` を用いて実行する |
| `Layers` | `SystemFlow → Interaction → 各 Function Port 抽象 ← 各 Function External 実装` という、「依存性逆転の原則」をベースにした4層のレイヤー構造 |
| `Blocks` | それぞれ、`SystemFlow Builder`、`Domain`、`External` と呼ばれる、レイヤーとは独立して存在する3つのブロック構造 |
| `System Flow Builder Block` | `SystemFlow` と依存オブジェクトを実行可能なスコープとして組み立てる |
| `External Block` | データベースやファイルシステム、UIなどの外部環境 |
| `Domain Block` | 外部環境に依存しない `System` の前提をデータ構造や計算で表す |

クリーンアーキテクチャとの違いの一つとして、`Domain` を中心、`External (Frameworks & Drivers 相当)` を外周とする同心円状の配置ではなく、両者を `Layers` とは独立した `Block` として扱う点が挙げられます。これにより、主要な依存経路と静的解析規則を単純に表現できます。

また、`System Flow Builder Block` が依存解決とライフタイムを管理するため、Program は個々の依存オブジェクトの生成手順に依存せず、実行環境の選択・構築と SystemFlow の実行に集中できます。

# Context Loop の実行経路 <a id="execution-path"></a>

この章では、Context Loop の実行経路を、汎用的な構造と Hello Door の具体例から確認します。

## Context Loop がライブラリ上で実行されるまで <a id="context-loop-execution"></a>

![Interaction Flow Architecture flow diagram](./img/InteractionFlowArchitecture_FlowDiagram.svg)

代替テキスト: [Interaction Flow Architecture - Flow Diagram Context](./img/InteractionFlowArchitecture_FlowDiagram.context.md)

この図は、User が体験する Context Loop (C1~3) と、Program が実行する System Flow の流れ (P1~6)、および User と System の相互作用の繰り返し (U1~2)、External と System の相互作用(E1~2) を1つの図にまとめたものです。


Context Loop は、`IFlowContext` が表現する `System` 側の Context と、`SystemFlow`/`Interaction`/`Function External` による実装、および `User` 側の行動選択によって表現されます。

表現された Context Loop は、`SystemFlowBuilder` や `SystemFlowHandler` を経由した `SystemFlow` の実行、その中での `Interaction` の実行、さらにその中での `Function` の実行による `System` 側の実行経路と、それに応じた `User` 側の行動によって実現されます。

Context Loop の実現過程の各時点では、`IFlowContext` が表現する状態が `User` や `External` との相互作用の結果として更新され、`SystemFlow` や `Interaction` の経路選択に影響を与えます。また、`Domain` による計算や状態を利用して `IFlowContext` を経由せずに `SystemFlow` や `Interaction` を選択する場合もあります。

Context Loop の実行過程をツリー形式で記述すると以下のようになります。

```text
Program　 :  SystemFlowBuilder で作成した SystemFlowHandler を通じて SystemFlow を実行する
  └─ SystemFlowHandler  :  SystemFlow の実行、破棄などの寿命を管理する
     └─ SystemFlow  :  IFlowContext や Domain を参照し、Interaction を選択、実行する
        └─ Interaction  :  IFlowContext や Domain を参照し、Function を選択、実行する
           ├─ Operation/Reaction  :  User との相互作用を目的とした内部・外部機能を提供する
           ├─ Storage  :  記録を目的とした内部・外部機能を提供する
           └─ Silent External  :  記録以外を目的とした内部・外部機能を提供する
```

ここで、1つの `Function` / `Interaction` / `SystemFlow` が完了しても、`Interaction` / `SystemFlow` / `Program` が終了するとは限りません。
それぞれの部分フローは、現在の `IFlowContext` インスタンス、さらなる部分フローの終了結果、Domain の状態などから継続や終了を判断します。

それぞれの代表的な具体例は以下の表のとおりです。
| 部分フロー | 責務 | 継続・終了の判断例 | 異常終了の具体例 |
| --- | --- | --- | --- |
| `Program` | 1つの `System` を実行する | 次の `SystemFlow` を選択・実行するか、1つの `System` として閉じるか | 回復不能な `SystemFlow` の失敗、初期化・実行不能な状態 |
| `SystemFlow` | 1つの `User` との関係を構築する | 次の `Interaction` を選択・実行するか、1つの `User` との関係として閉じるか | 回復不能な `Interaction` の失敗、異常な `IFlowContext`、異常な `Domain` |
| `Interaction` | 1つの相互作用の段階を進める | 次の `Function` を選択・実行するか、1つの相互作用の段階として閉じるか | 回復不能な `Function` の失敗、タイムアウト、未処理例外 |

## Hello Door で見る一周の流れ <a id="hello-door-flow"></a>

`InteractionFlow.Samples.HelloDoor` では、この実行経路は次ようになります。

```text
Program　 :  ScopeBuilder で、IDoorOperation/Reaction の実装を指定する
  |          SystemFlowBuilder で SystemFlowHandler を作成する
  |          最初の DoorState とそれを保持した IFlowContext を作成する
  |          作成した IFlowContext と SystemFlowHandler を組み合わせて実行する
  └─ SystemFlowHandler  :  DoorSystemFlow の実行、破棄などの寿命を管理する
     └─ DoorSystemFlow  :  OperateDoor を実行し、例外か終了要求があるまで繰り返す
        └─ OperateDoor  :  IDoorOperation の結果を IDoorReaction へ渡す
           ├─ IDoorOperation  :  User から DoorCommand を受け取る
           └─ IDoorReaction  :  DoorCommand を基に DoorState を更新し、結果を表示する
```

| 部分フロー | 責務 | 主なソース |
| --- | --- | --- |
| `Program` | `HelloDoor` を実行する | [`Program.cs`](../InteractionFlow.Samples.HelloDoor/Program.cs)、[`SystemFlowBuilder.cs`](../InteractionFlow.Standard/Builders/SystemFlowBuilder.cs)、[`SystemFlowHandler.cs`](../InteractionFlow.Core/Builders/SystemFlowHandler.cs)、[`FlowContext.cs`](../InteractionFlow.Core/Entities/Contexts/FlowContext.cs)、[`ScopedFlowContext.cs`](../InteractionFlow.Core/Entities/Contexts/ScopedFlowContext.cs)、[`DoorState.cs`](../InteractionFlow.Samples.HelloDoor/Entities/DoorState.cs) |
| `DoorSystemFlow` | `User` とドアの関係を構築する | [`DoorSystemFlow.cs`](../InteractionFlow.Samples.HelloDoor/SystemFlows/DoorSystemFlow.cs)、[`SystemFlow.cs`](../InteractionFlow.Core/SystemFlows/SystemFlow.cs) |
| `OperateDoor` | ドアの開閉操作を適用する | [`OperateDoor.cs`](../InteractionFlow.Samples.HelloDoor/Interactions/OperateDoor.cs)、[`Interaction.cs`](../InteractionFlow.Core/Interactions/Interaction.cs)、[`DoorCommand.cs`](../InteractionFlow.Samples.HelloDoor/Entities/DoorCommand.cs) |
| `IDoorOperation` | `User` 操作 を `DoorCommand` に変換する | [`IDoorOperation.cs`](../InteractionFlow.Samples.HelloDoor/ExternalPorts/OperationPorts/IDoorOperation.cs)、[`ConsoleDoorOperation.cs`](../InteractionFlow.Samples.HelloDoor/Externals/Operations/ConsoleDoorOperation.cs) |
| `IDoorReaction` | `DoorCommand` を適用して結果を `User` に伝える | [`IDoorReaction.cs`](../InteractionFlow.Samples.HelloDoor/ExternalPorts/ReactionPorts/IDoorReaction.cs)、[`ConsoleDoorReaction.cs`](../InteractionFlow.Samples.HelloDoor/Externals/Reactions/ConsoleDoorReaction.cs) |

ステップ形式の実装手順とコード全体は、
[Interaction Flow Architecture - Hello Door 🚪](../README.md#hello-door-) を参照してください。

Reaction による `Context` の更新は設計原則であり、ライブラリが必ず要求・保証するものではありません。
ライブラリによる保証範囲については [ライブラリが保証することと設計原則](#guarantees-and-principles) を参照してください。

# 目次

[全体像](#overview) | [Context Loop の実行経路](#execution-path) | [SystemFlow・Interaction・実行環境](#runtime-components) | [`IFlowContext` インスタンスと終了結果](#context-and-results) | [データ保持と永続化](#data-and-persistence) | [設計上の保証とプロジェクト境界](#architecture-boundaries) | [現在の制約と改善候補](#future-improvements)

[Interaction Flow Architecture](../README.md)
