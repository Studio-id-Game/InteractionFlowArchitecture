<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./PHILOSOPHY.md) |
[計算モデルとして](./COMPUTATIONAL_MODEL.md) |
[ライブラリ実装](./LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./LIBRARY_IMPLEMENTATION_DETAIL.md) |

---

# ライブラリの実装

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
- [実装の詳細](#implementation-details)
- [保証と制約と責務](#guarantees)

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

# 実装の詳細 <a id="implementation-details"></a>

各部の実装の詳細とアーキテクチャとの結び付きは、以下のドキュメントを参照してください。

- [ライブラリ実装の詳細](./LIBRARY_IMPLEMENTATION_DETAIL.md#implementation-details)
  - [システムの組み立て (System Flow Builder)](./LIBRARY_IMPLEMENTATION_DETAIL.md#system-flow-builder)
  - [値と処理結果の意味論 (Result, ReactionEnd, FlowEndToken, Entry)](./LIBRARY_IMPLEMENTATION_DETAIL.md#entry-result)
  - [文脈の構成 (Context)](./LIBRARY_IMPLEMENTATION_DETAIL.md#context)
  - [機能の分離 (Functions)](./LIBRARY_IMPLEMENTATION_DETAIL.md#functions)
  - [体験と相互作用のフロー (Interaction, SystemFlow)](./LIBRARY_IMPLEMENTATION_DETAIL.md#interaction-systemflow)

# 保証と制約と責務 <a id="guarantees"></a>

Interaction Flow Architecture の意味すべてを、C# の型だけで保証することはできません。
型・基底実装・Analyzer が支援する範囲と、設計・実装・レビューに委ねる範囲を区別します。

## Context 更新の原則 <a id="context-update-principle"></a>

相互作用としての影響の適用（つまり、次の相互作用に影響する `IFlowContext` の更新）は、`Reaction` または `Operation` 内で実施し、User が観測できる反応と対応させることを理想とします。
この理想的な制約は、このアーキテクチャの一番の目的である「ユーザー体験の質」とその設計のクリーンさを向上させます。

具体的な更新パターンの設計と、その理想性の対応は以下の表の通りです。

| 設計 | 設計の理想性 | 理由 |
| --- | --- | --- |
| Reaction が IFlowContext を更新し、結果を User に伝える | 理想的 | User と System が共有するべき認識を能動的に一致させるため |
| Reaction が IFlowContext を更新せずに、反応だけを User に伝える | 理想的 | User と System が共有するべき認識が変わらないため |
| Operation が操作を受け取って、IFlowContext を更新する | ほとんどの場合理想的 | User が操作を自覚しているなら、System 側の認識だけ更新すれば、共有するべき認識が一致するため |
| Operation が IFlowContext を更新せずに、受け取った操作を戻り値で返す | ほとんどの場合理想的 | 戻り値は、System 側が認識可能な、概念上の Context であると考えられるため |
| それ以外の Function、および Interaction 等の Function 以外の要素が IFlowContext を更新する | 可能であれば避けるべき | User の操作も System 側のからの通知も介さずに System 側の認識を更新すれば、共有するべき認識が乖離するため |

現在の所、これらの設計の理想性は、あくまでもアーキテクチャモデルを有効に活用するための原則であり、ライブラリが保証・制約するものではありません。
これは、単なる妥協としてではなく、実装上やむをえない場合などを想定した自由度のためのプレイスホルダーです。
実際の `IFlowContext` の更新設計が妥当であるかの判断は、上記の表を原則として、最終的にはライブラリ利用者の責務です。
この原則は、将来の運用実績から妥当だと判断できた時点で、ライブラリの制約として実装される可能性があります。

## その他の保証と制約と責務

| 内容 | 現在の実装 |
| --- | --- |
| SystemFlow が受け取る `IFlowContext` 実装型 | ジェネリック型制約と API が規定する |
| Interaction の正常完了時の戻り値 | `IInteraction` のメソッドシグネチャが `FlowEndToken` を規定する |
| 例外とキャンセルの Port への委譲 | `Interaction` 基底実装が提供する |
| `ReactionEnd` の生成経路 | `internal` コンストラクタと Reaction Port の `GetEnd` が制限する |
| Reaction が User に観測されること | 原理上保証できない |
| Context 更新が Reaction 内だけで行われること | 現在は保証しない |
| namespace の依存方向 | Analyzer が有効な場合に検査する |
| 意味的なレイヤー境界すべて | 現在の Analyzer では保証しない |

> [!要修正] [中]
> 以下に残る `[意味論]` / `[優先度]` のブロックは、移行中のレビュー課題を示す注記です。
> 完成版では、未反映の内容を本文へ統合し、既に本文へ反映済みの注記は削除してください。現状では一部の注記が直前の本文と矛盾しています。

> [意味論] [優先度：中] `LIBRARY_IMPLEMENTATION_OLD.md` 591–592行
> 型と Analyzer が保証しないという事実は本文にあるが、型による制約の目的が本文にない。
> 型による制約は設計判断を不要にするためではなく、README と Philosophy が示す User と System の関係をコード上でも追えるようにする支援である。

`InteractionFlow.Analyzers` は、namespace のレイヤー名から依存方向を検査する
`InteractionFlowArchitecture001` と、実行時依存グラフからの依存引数の欠落を検査する
`InteractionFlowArchitecture002` を提供します。Context 更新と Reaction の意味的対応、
複雑な依存グラフ全体は検査対象外です。

> [意味論] [優先度：中] `LIBRARY_IMPLEMENTATION_OLD.md` 605–608行
> Analyzer の診断内容は本文にあるが、コード編集時にアーキテクチャ境界を確認するための支援であるという導入目的が本文にない。
> `InteractionFlow.Analyzers` は、アーキテクチャの境界をコード編集中に確認するための Roslyn Analyzer である。

> [優先度：低] `LIBRARY_IMPLEMENTATION_OLD.md` 605–619行
> 対象レイヤー名を含まない namespace が検査対象外であること、および Analyzer の README への導線が移行で抜け落ちる。

| プロジェクト | 役割 |
| --- | --- |
| `InteractionFlow.Core` | Context、SystemFlow、Interaction、Port などの概念と基本契約 |
| `InteractionFlow.Standard` | DI、Console、FileSystem、Serializer などの標準実装 |
| `InteractionFlow.Samples.*` | Core と Standard を具体的な Context Loop として組み立て、API を検証 |

現在の主な検討事項は、Context 更新経路の制約、Entry／Context／Storage の所有権、
`ReactionEnd` と `Result` の内部表現、Function の実行時レイヤー情報、SystemFlow の終了結果、
同一スコープの並行実行、依存グラフの循環検査、Analyzer の対象範囲です。
これらは互換性を約束するロードマップではなく、利用例を通じて判断する設計上の課題です。

<details>
<summary>💡 Tips: Analyzer の有効化と例外経路</summary>

> Analyzer は `interactionflow_enabled = True` の場合に有効になります。
> このリポジトリでは `.editorconfig` により有効化し、診断モードを `Error` に設定しています。
>
> ```editorconfig
> [*.cs]
> # Interaction Flow Analyzer
> interactionflow_enabled = True
> interactionflow_mode = Error
> ```
> Exception Port が例外を再送出する設定の場合、または例外・キャンセル処理そのものが例外を送出した場合、
> `Interaction.ExecuteAsync` は `FlowEndToken` を返さず、例外が呼び出し側へ伝播します。
>
> 現在の改善候補には、同一スコープの並行実行時の状態分離・同期、依存グラフの循環検査と再合流表示、
> 非同期破棄を含む所有権モデル、namespace 外も含む意味的レイヤー判定があります。

</details>

# 目次

[全体像](#overview) |
[Context Loop の実行経路](#execution-path) |
[実装の詳細](#implementation-details) |
[保証と制約と責務](#guarantees)

[Interaction Flow Architecture](../README.md)

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./PHILOSOPHY.md) |
[計算モデルとして](./COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./LIBRARY_IMPLEMENTATION_DETAIL.md) |
