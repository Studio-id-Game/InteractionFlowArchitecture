[Interaction Flow Architecture](./README.md)

---

# System Flow Builder の設計

このドキュメントでは、Interaction Flow Architecture において、
なぜ `SystemFlow` の実行環境を Builder、Handler、Scope に分けているのかを説明します。

現在の型、API、組み立て手順については、
[ライブラリの実装 - システムの組み立て (System Flow Builder)](./docs/LIBRARY_IMPLEMENTATION_RESTRUCTURE_DRAFT.md#system-flow-builder)
を参照してください。

## System Flow Builder が解決する問題

`SystemFlow` は、一つ以上の `Interaction` を通じて、`System` が `User` との関係を構築するための単位です。
その実装を実行するためには、`Interaction`、`Function Port` の実装、Domain など、複数の依存オブジェクトを組み合わせる必要があります。

しかし、それらを `Program` が個別に生成すると、`Program` は実行環境の選択だけでなく、
各オブジェクトの生成順、共有範囲、破棄順まで知ることになります。
また、すべての依存を一つのスコープへ置くと、複数の `SystemFlow` で共有したい依存と、
一つの `SystemFlow` だけで使用したい依存の境界が曖昧になります。

System Flow Builder Block は、この問題に対して、次の責務を担います。

- `Program` が選択した実行環境から、`SystemFlow` と依存オブジェクトを組み立てる
- 共有する依存と `SystemFlow` 固有の依存を、別の Scope として構成する
- 構築中の登録状態と、構築後の実行状態を分離する
- 実行に使用する Scope の寿命を、明示的に扱える形で提供する

これは、汎用的な Dependency Injection の仕組みを定義すること自体が目的ではありません。
`SystemFlow` の設計を具体的な実行環境へ接続し、`Program` がその接続と寿命を選択できるようにするための仕組みです。

## アーキテクチャにおける位置

Interaction Flow Architecture の主要なレイヤー間の依存関係は、次のように表されます。

```text
SystemFlow -> Interaction -> Function Port <- Function External
```

System Flow Builder は、このレイヤーの一部ではなく、レイヤーを実行可能なオブジェクトとして組み立てる独立した Block です。
この分離により、`SystemFlow` と `Interaction` は具体的な DI コンテナや `Program` の構築手順に依存せず、
相互作用の選択と実行に集中できます。

また、System Flow Builder は `Context Loop` そのものではありません。
`Context Loop` は、`SystemFlow`、`Interaction`、`Function` による `System` 側の実行と、
それに応じた `User` の行動によって進行する過程です。
System Flow Builder は、その過程を実行するための `System` 側の構成を準備します。

## 構築と実行を分ける理由

現在の設計では、Builder が登録情報を受け取り、Handler が構築済みの実行対象を保持します。
この分離は、構築と実行を異なる状態として扱うためのものです。

Builder は、まだ変更可能な構成を表します。
一方、Handler は、特定の登録内容から構築された Scope と `SystemFlow` の組み合わせを表します。
実行時に登録内容を変更するのではなく、新しい構成が必要な場合は新しい Builder から新しい Handler を構築します。

この境界には、次の効果があります。

- 実行側が DI コンテナの構築手順を知る必要がない
- どの登録内容から現在の実行環境が作られたかを曖昧にしない
- Scope の構築途中に `SystemFlow` が実行される状態を作らない
- Handler の破棄を、実行環境の終了として扱える

Builder を一度 Build すると再利用できないのも、この一方向の状態遷移を保つためです。
再利用したい構成は、使用済み Builder を保持するのではなく、登録 Profile や構築済みの共有 Scope として表します。

## Scope を分ける理由

依存オブジェクトには、それぞれ異なる共有範囲があります。
複数の `SystemFlow` で共有したいものもあれば、一つの `SystemFlow` の実行環境だけに閉じたいものもあります。

この違いを表すため、現在の実装では、共通依存を保持する Scope と、
`SystemFlow` のために生成する専用 Scope を分けられるようにしています。
専用 Scope は、自身で解決できない依存を、指定された親 Scope から探索します。

```text
[SystemFlow 専用 Scope]
    |- SystemFlow 固有の依存
    `- fallback -> [共有 Scope]
                       `- 複数の SystemFlow で共有する依存
```

これにより、共通の構成を複製せずに再利用しながら、`SystemFlow` ごとの差分を専用 Scope に置けます。
専用 Scope に同じサービス型を登録すれば、共有 Scope の構成を変更せず、その `SystemFlow` だけで実装を差し替えられます。

ここでの親 Scope は、子 Scope が内容や寿命を継承する対象ではありません。
自身で解決できなかった依存を探索する、順序付きの参照先です。
複数の親を指定できるため、Scope の関係は単純な所有ツリーではなく、探索グラフになります。

この構成は柔軟性を与える一方で、探索順、循環、破棄順を設計上の関心に加えます。
そのため、Scope の合成は、依存の共有または差し替えという明確な理由がある範囲に留めることが望まれます。

## Scope と所有権を一致させすぎない理由

Scope は、依存解決と、その DI Scope が追跡する依存オブジェクトの寿命を扱います。
しかし、実行に関係するすべてのオブジェクトを所有するわけではありません。

現在の主な関係は、次のとおりです。

```text
Program
  |- ScopeHandler を必要な期間保持する
  |- SystemFlowHandler を必要な期間保持する
  |    |- SystemFlow を実行対象として保持する
  |    `- 専用 ScopeHandler を破棄する
  `- IFlowContext を生成し、実行時に渡す

ScopeHandler
  |- 自身の DI Scope を破棄する
  `- 親 ScopeHandler は探索するだけで、破棄しない
```

`SystemFlowHandler` を破棄すると、専用 `ScopeHandler` が破棄され、以後の実行は無効になります。
一方で、親 `ScopeHandler` や、実行時に外部から渡された `IFlowContext` インスタンスは破棄しません。

また、DI Scope の破棄によって破棄されるのは、その DI 実装が追跡している破棄可能なサービスです。
`SystemFlowHandler` は、保持している `SystemFlow` 自体に対して `Dispose` を呼び出しません。
外部から渡したオブジェクトや `SystemFlow` が独自に所有するリソースについては、別途所有者を決める必要があります。

すべてを一つの所有権へ統合しないことで、共有 Scope や `IFlowContext` を複数の実行に利用できます。
その代わり、どのオブジェクトを誰が破棄するかは、Scope の境界だけから自動的に決まるとは限りません。

## 明示的な寿命管理を選ぶ理由

`ScopeHandler` と `SystemFlowHandler` は `IDisposable` を実装し、利用者が実行環境の終了を明示します。
これは、依存オブジェクトが不要になる時点を GC や暗黙的なコンテナ所有権だけに委ねず、
`Program` の制御フローから確認できるようにするためです。

明示的な破棄によって、次の境界を表現できます。

- 共有 Scope を、複数の `SystemFlow` より長く保持する
- `SystemFlow` 固有の Scope を、個別の Handler と共に終了する
- 先に子を破棄し、その後で共有する親を破棄する
- 破棄済みの実行環境を再度使用しない

親 Scope の寿命は子 Scope から独立しています。
子を破棄しても親は利用できますが、親を先に破棄した後で子が親の依存を探索すると例外になります。
この非対称性によって共有 Scope の再利用が可能になる一方、破棄順は `Program` の責務になります。

## Context 型を構築境界に置く理由

`SystemFlowBuilder<TContext>` と `SystemFlowHandler<TContext>` は、`SystemFlow` が扱う `IFlowContext` の実装型を型引数に持ちます。
これにより、依存オブジェクトの構成だけでなく、構築した `SystemFlow` がどの Context 契約で実行されるかも、
Handler の型として残ります。

この型境界は、`IFlowContext` を `Context` や `Context Loop` そのものとして所有するためのものではありません。
`IFlowContext` は、概念上の `Context` のうち `System` 側で扱う文脈を提供する実装上の投影です。
Handler はそのインスタンスを実行時に受け取りますが、所有も破棄もしません。

そのため、依存 Scope を共有することと、同じ `IFlowContext` インスタンスを共有することは別の設計判断です。
特に並行実行では、同一 Scope 内の可変な依存オブジェクトと `IFlowContext` の安全性は保証されません。
通常は同一 Scope の実行を逐次化し、並行して実行する場合は Scope と `IFlowContext` を分けます。

## 設計判断とトレードオフ

| 設計判断 | 得られるもの | 引き受ける制約 |
| --- | --- | --- |
| Builder と Handler を分ける | 構築中と実行中の状態を区別できる | 構築と実行に別の型が必要になる |
| Builder を一度だけ Build する | 登録状態と生成された Scope の対応が明確になる | Builder 自体は再利用できない |
| `SystemFlow` ごとに専用 Scope を持てる | Flow 固有の依存と寿命を分離できる | Scope の境界を設計する必要がある |
| 親 Scope を探索する | 共通依存を複製せず再利用できる | 探索順、循環、破棄順を考慮する必要がある |
| Handler を明示的に破棄する | 実行環境の終了をコード上で示せる | 利用者が破棄責任を持つ |
| Context 型を Handler に残す | `SystemFlow` と Context 契約の対応を型で表せる | 実行時に対応する Context を用意する必要がある |

System Flow Builder の目的は、この表の一方だけを最大化することではありません。
共有と分離、再利用と所有権、構築時の柔軟性と実行時の安定性の間に、明示的な境界を置くことです。

## 適用範囲と非目標

現在の System Flow Builder は、次のことを目的としていません。

- 汎用 DI コンテナのすべての機能を抽象化すること
- `Context Loop` やユーザー体験そのものを表現すること
- 実行に関係するすべてのオブジェクトへ統一的な所有権を与えること
- 同一 Scope と `IFlowContext` の並行利用を安全にすること
- Scope の循環や不適切な寿命関係を構築時にすべて検査すること
- 親 Scope や、外部から渡された `IFlowContext` を自動的に破棄すること

これらを非目標として残すことで、Core は特定の DI コンテナへ依存しない最小限の構築・実行契約を持ち、
Standard は現実の利用に合わせた具体的な DI 実装を提供できます。

## 現在の制約と検討点

現在の設計には、次の検討点があります。

- 同一 Scope を並行実行する場合の状態分離と同期
- `IAsyncDisposable` を含む非同期破棄
- `SystemFlow` 自体が破棄可能なリソースを持つ場合の所有権
- 依存探索グラフの循環検査と、複数経路が再合流する場合の扱い
- Core の抽象契約と、Standard が使用する DI コンテナ固有機能との境界

これらは将来の実装を約束するロードマップではありません。
現在の設計で意図的に単純化している範囲と、利用例を通じて判断する必要がある課題を示しています。

---

[Interaction Flow Architecture](./README.md) |
[ライブラリの実装](./docs/LIBRARY_IMPLEMENTATION.md) |
[System Flow Builder の実装](./docs/LIBRARY_IMPLEMENTATION_RESTRUCTURE_DRAFT.md#system-flow-builder)
