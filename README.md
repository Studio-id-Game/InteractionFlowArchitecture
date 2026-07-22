# Interaction Flow Architecture　<a id="top"></a>

<p align="center">
    <img
        src="./docs/icon/icon_flat.svg"
        alt="Interaction Flow Architecture icon"
        width="128"
    >
    <p align="center">
    <i>
        Interaction shapes Context, <br>
        and Context shapes Interaction.
    </i>
    </p>
</p>

<br>

<p align="center">
    このアーキテクチャは、<br/>
    開発対象となるシステムを「コードやレイヤーの塊」として見るだけではなく、<br/>
    <b>「ユーザーとシステムの境界で生まれる相互作用」と「循環する文脈」</b> をコード表現に落とし込み、<br/>
    ユーザー体験を直接設計するためのアーキテクチャです。
</p>


- [パッケージとインストール](#packages)
- [ビジョン](#vision)
- [Context Loop & System Flow](#context-loop-system-flow)
- [哲学](#philosophy)
- [はじめに](#getting-started)
- [サンプル](#examples)
- [実装](#implementation)
- [ロードマップ](#roadmap)
- [補足資料](#references)
- [確認が必要な点](#open-questions)

---

# パッケージとインストール <a id="packages"></a>

このリポジトリは、Interaction Flow Architecture を C# / .NET で実装するための、
ベースライブラリやAnalyzerのパッケージ、サンプルプログラム等を提供します。

`Core`、`Standard`、`Samples` の詳細な責務と更新方針は [Core / Standard / Samples の役割](./docs/RoleOfMainProjects.md) を参照してください。

## Core
- `InteractionFlow.Core` は、アーキテクチャ概念を定義する最小パッケージです。
- Target Framework: `netstandard2.1` 以上
- Installation:
  ```bash
  dotnet add package InteractionFlow.Core
  ```

## Standard
- `InteractionFlow.Standard` は、Core に現実のユースケースで扱いやすい機能を加えた標準パッケージです。
- Target Framework: `netstandard2.1` 以上
- Installation:
  ```bash
  dotnet add package InteractionFlow.Standard
  ```
**通常はこのパッケージをインストールするのが最短です。**

## Analyzer
- `InteractionFlow.Analyzers` は、Interaction Flow Architecture のルールを検査する Roslyn Analyzer です。
- Target Framework: `netstandard2.0` 以上
- Installation:
  ```bash
  dotnet add package InteractionFlow.Analyzers
  ```
  
**より強力なアーキテクチャ支援を受けるために導入を推奨します。（プロジェクトファイルから `PrivateAssets="all"` を付ける事を推奨）**

標準的なインストール:
```xml
<ItemGroup>
  <PackageReference Include="InteractionFlow.Standard" Version="0.5.0" />
  <PackageReference Include="InteractionFlow.Analyzers" Version="0.5.0" PrivateAssets="all" />
</ItemGroup>
```

## Samples
- `InteractionFlow.Samples.*` は、Architecture の使い方を具体例として確認するためのプロジェクト群です。
- 個別サンプルの目的と読み方は [サンプル解説](#examples) にまとめています。

## Others

- `InteractionFlow.PackageInstallCheck` は、リポジトリ運用のための補助プロジェクトです。

---

# ビジョン <a id="vision"></a>

## なぜ、新しいアーキテクチャが必要なのか？

> <I>相互作用をクリーンに保つための制約を持ったアーキテクチャ。</I>

レイヤードアーキテクチャやクリーンアーキテクチャは、コードの依存方向や責務分離を整理する強力な考え方です。一方で、これらのアーキテクチャモデルは、モデル上の要素と実際のコードとの対応関係を強くは定めません。

そのため、設計上の境界とコード上の境界の一致を継続的に保証することは難しく、両者の間に乖離が生じる可能性があります。また、同じ名称や図を共有していても、開発者ごとに異なる対応関係を想定できるため、解釈の余地が設計の自由度としてだけでなく、認識の曖昧さとしても現れます。

特に、対話的な UI、ゲームループ、エージェントなど、複雑な入力と出力を持つシステムでは、処理が複数のレイヤーを横断しながら、継続的に状態を更新し、次の振る舞いを形成します。そのため、アーキテクチャモデル上の責務分割だけでは、実際のコードがどのような実行フローを形成するのかを十分に表現できず、コードとモデルの対応関係はさらに曖昧になります。

このようなインタラクティブなシステムの開発において、開発者が本当に設計したいものは、「どのクラスをどのレイヤーに置くか」だけではありません。ユーザーが何を行い、システムがそれをどう受け取り、その結果として文脈がどう変化し、次の相互作用がどのように形づくられるかという、相互作用の流れによる「ユーザー体験（UX）」そのものです。

Interaction Flow Architecture は、レイヤー構造による責務分離を維持しながら、相互作用をコード上の基本単位として設計します。これにより、モデルとコードの対応関係を明確に保ちつつ、ユーザーとシステムの間に生まれる相互作用の流れ＝「ユーザー体験」を、そのままコードとして記述できます。

このアーキテクチャでは、コードを実行した結果としてユーザー体験が生まれるのではなく、コードそのものがユーザー体験の設計言語になります。

Interaction Flow Architecture は、単なるコードの整理規則ではありません。**「ユーザー体験」をクリーンに保つための制約を持ったアーキテクチャ**です。

## コアコンセプト

> <I>相互作用が文脈を形作り、文脈が相互作用を形作る。</I>

Interaction Flow Architecture では、
ユーザーとシステムの相互作用（`Interaction`）と、相互作用の状態を表す文脈（`Context`）を用いて、相互作用が文脈を更新し、新しい文脈が次の相互作用に影響する過程の繰り返し（`Context Loop`）として、システムを表現します。

```text
Context -> Interaction -> next Context -> next Interaction -> ...
```

このアーキテクチャでは、次の言葉を共通言語として使います。

### 基本概念:

- `User`: System と相互作用する主体。人間だけでなく、ロボット、AI エージェント、動物など、様々な主体を含む。
- `Context`: 現在の相互作用に関する状態や状況に、次の相互作用に影響を与える文脈的な意味を持たせた情報。
- `System`: `User` と相互作用する開発対象。`Context` を介して `User` の行為に反応し、動作する。
- `Context Loop`: `System` と `User` の間にある、 `Context` を介した繰り返しの反応プロセス。
- `System Flow`: `Context Loop` の一環として、複数の相互作用を通じて `System` が `User` との関係を構築するための単位。
- `Interaction`: `Context Loop` の一環として、`System` が内部の目的を達成するための相互作用の単位。

### 実装概念:

- `Function`: `Interaction` から呼び出される外部機能の単位。
  
  `Interaction` が扱う抽象的な契約である `Function Port` と、<br/>
  `External` に依存した具体的な実装である `Function External` がある。

  - `Operation`: `Function`の一種。`User` が操作できる入力を受け付ける機能。
  - `Reaction`: `Function`の一種。`User` が観測できる反応を提供する機能。
  - `Storage`: `Function`の一種。`Context` の文脈的な意味とは独立して、データを保持する機能。
  - `Silent External`: `Function`の一種。`Context` の文脈的な意味とは独立して、外部と連携するその他の機能。

- `System Flow Builder`: `System Flow` を実行可能な形に組み立てるビルダー。
- `Domain`: `System` の前提となる、外部に依存しないデータ構造や動作の定義。
- `External`: UI、DB、ファイルシステム、OS、外部サービスなど、`Function External` が接続する具体的な実行環境。

### 説明概念:

- `Meta Context`: 開発者や AI が設計意図を理解するために読む文脈。

  ドキュメント、図、命名、型、コメントなどの、設計意図を理解するために読まれる開発時のみの文脈です。
  `Meta Context` と 通常の `Context` を明示的に対比したい箇所では、通常の `Context` を `Runtime Context` と特別に呼びます。


#### アーキテクチャの全体図:
![Interaction Flow Architecture overview](./docs/img/InteractionFlowArchitecture_Overview.svg)

代替テキスト: [InteractionFlowArchitecture_Overview.context.md](./docs/img/InteractionFlowArchitecture_Overview.context.md)

---
<br/>

# Context Loop & System Flow <a id="context-loop-system-flow"></a>

## Context Loop の具体例

> <I>さぁ、ドアを開けて。</I>

最小の Context Loop は、ドアの例で考えると直感的です。

🚪 ドアを開閉する (Open / Close the Door)
```text
# ドアの Context Loop

1. Operation : User がドア（System）を操作する（Open / Close）
2. Reaction : その操作と、ドアの開閉状態（Context）を参照して…
     case A : ドアが Close で入力が Open なら、ドアを開ける
     case B : ドアが Open で入力が Open なら、ドアはすでに開いている
     case C : ドアが Open で入力が Close なら、ドアを閉める
     case D : ドアが Close で入力が Close なら、ドアはすでに閉まっている
3. ドアの開閉状態（Context）を引き継いだまま、(1.)に戻る
```

このドアの Context Loop は、 1つの Operation と 1つの Reaction を組み合わせた、1つの Interaction が繰り返されるループとして記述されています。

この Interaction の結果は、同じ `Open` という操作（Operation）でも、ドアの開閉状態（Context）が「Close」なら開けられ、「Open」ならすでに開いているという反応（Reaction）になります。そして、ここで更新されたドアの開閉状態（Context）は、次の Interaction を変化させます。

このように、

1. Interaction が、
2. Operation や Reaction を通じて Context を更新し、
3. 次の Interaction を変化させる。

というループが、Context Loop の基本となります。

また、System Flow は、複数の Interaction をまとめることで、System における1つのユーザー体験として Context Loop を表現します。ドアの Context Loop の例は、1つの Interaction で構成される最小の System Flow であるとも言えます。

## User から見た Context Loop & System Flow

> <I>User は、System との関係を構築する。</I>

User は、ドアの Context Loop を次のように体験します。

1. ドア（System）を見つける
2. ドアの操作（Operation）を実行する
3. 操作の結果を表す反応（Reaction）を受け取る
4. (2.)と(3.)を繰り返すことで、ドアの状態（Context）を理解しながら操作できるようになる

特に、(4.) で経験する Context Loop によって、User はドア（System）との関係を構築します。
これこそが、System Flow が「System が User との関係を構築するための単位」である理由と目的です。

また、(2.) Operation と (3.) Reaction は User と System で実行・観測する立場が反転します。
- User は Operation を実行し、System は User の Operation を受け取る
- System は Reaction を実行し、User は System の Reaction を受け取る

## 開発者から見た Context Loop & System Flow

> <I>開発者は、User との関係をデザインする。</I>

開発者は、ドアの Context Loop を次のように設計します。

1. ドア（System）の状態（Context）を定義する
2. ドアの操作（Operation）を定義する
3. ドアの反応（Reaction）を定義する
4. 操作と反応を組み合わせて、ドアの相互作用（Interaction）を実装する
5. Interaction を組み合わせて、ドアと User との関係（System Flow）をデザインする

特に、(5.) でデザインする System Flow は、ユーザーが体験する Context Loop として実装します。

![Interaction Flow Architecture flow diagram](./docs/img/InteractionFlowArchitecture_FlowDiagram.svg)

代替テキスト: [InteractionFlowArchitecture_FlowDiagram.context.md](./docs/img/InteractionFlowArchitecture_FlowDiagram.context.md)

## 共有される Context Loop & System Flow

> <I>Context Loop と System Flow によって、開発者 / System / User が同じ世界を共有する。</I>

開発者によりデザインされた System Flow と、それによって User が体験する Context Loop は、開発者 / System / User が同じ世界を共有するための共通モデルとなります。

- 開発者は、System Flow をデザインし、Context Loop として実装する
- System は、System Flow を実行し、Context Loop を提供する
- User は、System Flow の中で、Context Loop を体験する

冒頭で、インタラクティブなシステムの開発において、開発者が本当に設計したいものは「ユーザー体験」であると述べました。

ここで述べた Context Loop こそが、実行モデルとしての「ユーザー体験」であり、System Flow のデザインこそが、「ユーザー体験の設計」となります。**System Flow のデザインによって、開発者がユーザー体験を直接設計できるようになる**ことが、Interaction Flow Architecture の最大の利点です。

---


## Context を設計する (Designing Context)

Context は、フローの現在地を表す文脈です。単なる mutable state ではなく、次の Interaction を決めるために共有される情報です。

設計時は、次の境界を明確にします。

- `FlowContext`: フローに渡される基本の文脈。
- `ScopedFlowContext`: フロー中だけ追加される一時的な文脈。
- Domain Entity: System の前提として外部に依存しない概念。
- Storage Data: 保存、復元、共有の対象になる情報。

Context は広げすぎると何でも入る袋になります。狭すぎると Interaction 間の関係がコード外に漏れます。所有者、ライフサイクル、更新理由が説明できる範囲で定義します。

## Context の更新を制御する (Controlling Context Updates)

Context は誰でも自由に更新できるものではありません。

- `Function` は、Port / External の境界を通じて外部機能を扱う。
  - `Operation` は、User 入力や外部条件を読み取る。
  - `Reaction` は、User へ結果を返し、終了状態を表す。
  - `Storage` は、永続または一時の状態を保存、復元する。
  - `SilentExternal` は、User に直接見えない外部状態や外部イベントを扱う。
- `Interaction` は、これらの Function Port を組み合わせて Context の意味ある更新を構成する。
- `SystemFlow` は、Interaction の順序と終了の意味を構成する。

この分担により、Context 更新の理由が Interaction Flow 上に残ります。

## デザイン言語としてのコード (Code as a Design Language)

コードは、Architecture の説明そのものです。

Interaction Flow Architecture では、コードを書くこと自体が System 設計になります。API、型、命名、責務は実装詳細ではなく、User 体験をどの単位で分け、どの Context を次へ渡すかを表す設計語彙です。

### 責務 (Responsibilities)

各クラス、各コンポーネントは一つの責務だけを持ちます。

`SystemFlow` は Interaction を束ねます。`Interaction` は Port を束ねます。`Function Port` は外部機能を抽象化します。`Function External` は具体的な外部依存を扱います。`Domain` は外部に依存しない前提を定義します。

責務が混ざると、Context がどこで形成され、どこで更新されたのかが読めなくなります。

### API設計 (API Design)

API は Interaction Flow を自然に表現できる形にします。

このリポジトリでは、`SystemFlow<TContext>`、`Interaction`、`IFlowContext`、`FlowEndToken`、`ReactionEnd`、`ScopeBuilder`、`SystemFlowBuilder` などが、フローの意味をそのままコードに写すための基盤になっています。

Builder は DI コンテナの詳細を隠すのではなく、SystemFlow とその依存スコープを明示的に組み立てるために使います。

Builder のスコープ構造、親スコープとの合成、ライフタイム管理の詳細は、[SystemFlow Builder の詳細](./docs/SystemFlowBuilder.md) にまとめています。

### 命名 (Naming)

命名は、Interaction と Context を中心にします。

- User への反応プロセスは `SystemFlows` に置く。
- システム内部の意味単位は `Interactions` に置く。
- 外部依存の抽象は `ExternalPorts` に置く。
- 外部依存の実装は `Externals` に置く。
- 外部に依存しない概念は `Entities` に置く。
- 構築処理は `Builders` に置く。

名前空間とディレクトリを一致させることで、構造がそのままドキュメントになります。

### 型 (Types)

型は、設計意図を表すために使います。

Context の型は、どの SystemFlow が何を前提に実行されるかを表します。Port の型は、Interaction がどの外部機能を必要としているかを表します。Domain の型は、System が外部に依存せずに保持する概念を表します。

型によって責務を表すことで、アーキテクチャの境界をコード補完、コンパイル、Analyzer に乗せられます。

### ドキュメント (Documentation)

ドキュメントは、実行時の Context ではなく Meta Context の一部です。

README は、プロジェクト全体の共有 Meta Context です。図は構造を短時間で復元するために残し、コメントはコードだけでは読み取れない設計判断に絞って残します。

ドキュメントの責務は、実装そのものを繰り返すことではなく、開発者や AI が設計意図を復元できる文脈を保つことです。

### Analyzer

Analyzer は、人間の注意力だけに依存せず、アーキテクチャのルールを検査するための仕組みです。

依存関係の規約、Layer / Block の境界、Port と External の分離は、レビューだけで守るには忘れやすいものです。Analyzer によって設計ルールを開発環境に近づけることで、Architecture を「読んで守るもの」から「書きながら支援されるもの」にします。

## AIとの協調 (AI Collaboration)

AI は Meta Context を読み取り、Context を扱うコードを補助する存在です。

Interaction Flow Architecture は、AI に渡す Meta Context を圧縮します。ファイル構成、名前空間、図、README、Analyzer のルールが揃っていると、AI は「この変更はどの責務に属するか」「どの境界を越えてはいけないか」を短い文脈で復元できます。

ここでの焦点は、何をドキュメントに残すかではなく、残された Meta Context を使って AI がどのように協調できるかです。Architecture は AI のためだけにあるものではありません。人間と AI が読む量を減らしたうえで、同じ責務境界を共有するための技術です。

人間はモデルを設計し、AI はモデルを展開します。人間は意味と境界を決め、AI はその境界の内側で実装、検証、反復を支援します。AI に依存しない構造を先に置くことで、AI を安全に活用しやすくなります。

![Interaction Flow Architecture dependency diagram](./docs/img/InteractionFlowArchitecture_DependencyDiagram.svg)

代替テキスト: [InteractionFlowArchitecture_DependencyDiagram.context.md](./docs/img/InteractionFlowArchitecture_DependencyDiagram.context.md)

---

<a id="runtime-development"></a>

# ランタイム × 開発 (Runtime × Development)

## 二つの視点 (Two Perspectives)

Runtime と Development は、別々のループを持つのではありません。同じ Interaction と Context のループを、別の立場から見ています。

Runtime では、User がそのループを体験します。User は何かを行い、System から反応を受け取り、更新された Context によって次の Interaction が変わることを体験します。

Development では、開発者が同じループを実装として設計します。どの Context を持ち、どの Interaction がそれを読み、どの Function Port を通じて Operation / Reaction / Storage / SilentExternal を扱うかを決めます。

```text
Shared loop:
  Interaction -> Context -> next Interaction -> next Context -> ...

Runtime:
  User experiences the loop.

Development:
  Developer designs the same loop.
```

Meta Context は、この同じループを開発者や AI が短く復元するために残す文脈です。

## 一つの共有モデル (One Shared Model)

共有する中心概念は二つです。

- `Interaction Flow`
- `Context`

Users experience it.
Developers design it.
AI learns it.

User は Interaction Flow と Context の変化を体験します。開発者は同じ Interaction Flow と Context を設計します。AI は Meta Context からその構造を学習し、実装や検証を補助します。

この三者が同じ Interaction / Context Loop を共有できることが、このアーキテクチャの実用上の価値です。開発者が実装を考えるときにも、「User は今どの Context にいて、どの Interaction を行い、どの Reaction を受け取るのか」を基準にできます。

その結果、ユーザー目線の開発が特別な工程ではなく、日常の設計判断になります。クラス分割、Port の境界、Storage の扱い、Reaction の設計は、すべて User Flow を保つための選択として説明できます。実装の都合で User Flow が見えなくなったときも、共有された Interaction と Context に戻ることで、設計判断を立て直せます。

---

<a id="philosophy"></a>

# 哲学 (Philosophy)

<p align="center">
  <img
    src="./docs/img/illustration/ChatGPT_Header01_s.png"
    alt="Interaction Flow Architecture concept illustration"
    width="100%"
  >
</p>

<p align="center">
  <i>
    Interaction shapes Context, <br>
    and Context shapes Interaction.
  </i>
</p>

## Interaction

Interaction は、User と System の間に起きる作用であり、System 内部では目的を持った状態遷移のまとまりです。

Interaction は、User が一方的に実行する命令でも、System が一方的に返す処理でもありません。User と System の境界で成立し、現在の Context によって成立条件と意味が決まります。

Interaction は単なる関数呼び出しではありません。現在の Context の中で成立し、その結果として Context を次へ進める意味単位です。実装上の分解は Development の章で扱います。

このアーキテクチャが Interaction を中心に置くのは、User 体験が Interaction の連続として現れるからです。

## Context

Context は、現在の SystemFlow に関する状態、状況、文脈的情報です。

Context は state と似ていますが、意味が少し違います。state は値そのものに注目します。Context は、その値が次の Interaction をどう変えるかに注目します。

たとえば「ログイン済み」という値は state です。その値によって「ノート一覧を表示できる」「編集できる」「ログイン画面に戻す」といった次の Interaction が決まるとき、それは Context として働きます。

## 共有 Meta Context (Shared Meta Context)

人、AI、System は、それぞれ違う形で Context と Meta Context を扱います。

System は Context を使って Interaction を進めます。User はその結果を体験として受け取ります。開発者は Context をコードとして設計し、その設計意図を README、図、命名、型、コメントという Meta Context に残します。AI はその Meta Context を読み取り、Context を扱う実装を補助します。

共有 Meta Context が明確であるほど、協調は楽になります。説明が短くなり、誤解が減り、変更の影響範囲が見えやすくなります。

## アーキテクチャは共有 Meta Context である (Architecture as Shared Meta Context)

Architecture は、コードのルール集だけではありません。

Architecture は、チームと AI をつなぎ、User 体験の設計判断を共有する Meta Context です。どこに何を書くか、どの責務を混ぜないか、どの言葉で設計を語るかを揃えることで、User 体験と実装の構造が同じ方向を向きます。

Interaction Flow Architecture は、User の体験、開発者の実装、AI の理解を、Interaction、Context、Meta Context の関係として整理します。

## 計算モデルとしての補助線 (Computational Model)

この節は、Interaction Flow Architecture の中心思想を置き換えるものではなく、実装モデルを別の角度から検証するための補助資料です。

Interaction Flow Architecture の Function (Port / External) は、チューリングマシンとしても説明できます。

この見方では、Function Port / Function External の子要素である Operation / Reaction / Storage / SilentExternal は、「読み取り、書き込み、外部への作用」を担うテープ操作に相当します。Interaction と Context の循環を、計算モデルとして検証しやすくするための見方です。

詳しい説明は [計算モデルとしての Interaction Flow アーキテクチャ](./docs/ComputationalModel.md) を参照してください。

---

<a id="packages"></a>

# パッケージ (Packages)

主要な役割分担として、抽象と契約を置く `Core`、再利用可能な標準実装を置く `Standard`、具体例と検証を担う `Samples` を分けています。各プロジェクトの責務と更新方針は [Core / Standard / Samples の役割](./docs/RoleOfMainProjects.md) にまとめています。

## Core

`InteractionFlow.Core` は、アーキテクチャ概念を構造と振る舞いとして定義する最小パッケージです。

- Target Framework: `netstandard2.1`
- 主な要素: `SystemFlow`、`Interaction`、`IFlowContext`、`FlowContext`、`ScopedFlowContext`、`FlowEndToken`
- Port: `IOperationPort`、`IReactionPort`、`IStoragePort`、`ISilentExternalPort`
- 役割: 外部実装や UI に依存しない Core の契約を提供する

Core には、Standard や Samples に依存する実装を入れません。

## Standard

`InteractionFlow.Standard` は、Core を現実のユースケースで扱いやすい形に整えた標準実装です。

- Target Framework: `netstandard2.1`
- DI: `ScopeBuilder`、`SystemFlowBuilder`、`ScopeHandler`、`SystemFlowHandler`
- Console: コンソール Operation / Reaction / SilentExternal の標準実装
- FileSystem: ファイル、ディレクトリ永続化の標準実装
- Serialization: stream / text serializer の標準実装

通常は `InteractionFlow.Standard` から始めるのが最短です。

## Analyzer

`InteractionFlow.Analyzers` は、Interaction Flow Architecture の依存関係ルールを検査する Roslyn Analyzer です。

- Target Framework: `netstandard2.0`
- 役割: Layer / Block の境界、依存関係の規約、アーキテクチャ違反を開発時に検出する
- 推奨: アプリケーション側では `PrivateAssets="all"` を付けて参照する

## Samples

`InteractionFlow.Samples.*` は、Architecture の使い方を具体例として確認するためのプロジェクト群です。

- 役割: Runtime の Context Loop が Development の実装構造へどう写るかを確認する
- 方針: 実験的な使い方や検証を Samples 側に閉じ込め、Core / Standard の責務を濁さない
- 導線: 個別サンプルの目的と読み方は [サンプル (Examples)](#examples) にまとめています

## PackageInstallCheck

`InteractionFlow.PackageInstallCheck` は、パッケージ導入確認用の補助プロジェクトです。Samples の学習用フローではなく、公開パッケージとして参照したときに最小構成が成立するかを確認するために使います。

---

<a id="getting-started"></a>

# はじめに (Getting Started)

## インストール (Installation)

NuGet から利用する場合は、標準実装を含む `InteractionFlow.Standard` から始めます。

```bash
dotnet add package InteractionFlow.Standard
```

設計ルールの検査も有効にする場合は Analyzer を追加します。

```bash
dotnet add package InteractionFlow.Analyzers
```

プロジェクトファイルに直接書く場合は、Analyzer に `PrivateAssets="all"` を付けることを推奨します。

```xml
<ItemGroup>
  <PackageReference Include="InteractionFlow.Standard" Version="0.4.1" />
  <PackageReference Include="InteractionFlow.Analyzers" Version="0.4.2" PrivateAssets="all" />
</ItemGroup>
```

このリポジトリを直接動かす場合は、.NET SDK `9.0.313` 以降の latest feature roll-forward が使われます。

```bash
dotnet build InteractionFlow.slnx
```

## Hello Door 🚪

Hello Door は、最初の Context Loop を理解するための最小例です。単なる Hello World ではなく、User が System へ入る最小の入口 (Entrance) として扱います。

```text
Context:
  Door is closed

Interaction:
  Operate the Door

Operation:
  User inputs "Open" or "Close"

Reaction:
  "The door opens."

Updated Context:
  Door is open
```

このサンプルでは、Operation と Reaction を Console 外部依存として実装し、`Program` クラスから SystemFlow を実行します。実装は `InteractionFlow.Samples.HelloDoor` にあり、README のコードはファイル単位で同じ責務に対応しています。

実行する場合は、次のコマンドを使います。

```bash
dotnet run --project InteractionFlow.Samples.HelloDoor/InteractionFlow.Samples.HelloDoor.csproj
```

**Step 1.** User 入力を Interaction が扱うコマンドとして定義します。

`Entities/DoorCommand.cs`

```csharp
namespace InteractionFlow.Samples.HelloDoor.Entities
{
    internal enum DoorCommand
    {
        Open,
        Close,
        Exit,
        Unknown,
    }
}
```

**Step 2.** Context に載せるドアの状態を定義します。`IsOpen` は現在の開閉状態、`ExitRequested` は SystemFlow のループ終了要求です。

`Entities/DoorState.cs`

```csharp
namespace InteractionFlow.Samples.HelloDoor.Entities
{
    internal sealed class DoorState
    {
        public bool IsOpen { get; set; }

        public bool ExitRequested { get; set; }
    }
}
```

**Step 3.** Interaction から見える入力 Port を定義します。Interaction は Console を直接読まず、この Port だけを呼びます。

`ExternalPorts/OperationPorts/IDoorOperation.cs`

```csharp
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.OperationPorts;
using InteractionFlow.Samples.HelloDoor.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.ExternalPorts.OperationPorts
{
    internal interface IDoorOperation : IOperationPort
    {
        ValueTask<DoorCommand> ReadCommandAsync(IFlowContext context);
    }
}
```

**Step 4.** Interaction から見える出力 Port を定義します。直前に入力された `DoorCommand` を受け取り、`DoorState` Context の更新と結果表示を担当します。

`ExternalPorts/ReactionPorts/IDoorReaction.cs`

```csharp
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Samples.HelloDoor.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.ExternalPorts.ReactionPorts
{
    internal interface IDoorReaction : IReactionPort
    {
        ValueTask<ReactionEnd> ReactAsync(IFlowContext context, DoorCommand command);
    }
}
```

**Step 5.** 入力 Port を Console で実装します。ここが Operation の External 実装です。

`Externals/Operations/ConsoleDoorOperation.cs`

```csharp
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Operations;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.OperationPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.Externals.Operations
{
    internal sealed class ConsoleDoorOperation : Operation, IDoorOperation
    {
        public override void ForceResetMemoryState()
        {
        }

        public ValueTask<DoorCommand> ReadCommandAsync(IFlowContext context)
        {
            Console.Write("Door command (Open/Close, Enter to exit): ");
            var text = Console.ReadLine()?.Trim();

            return new(text?.ToUpperInvariant() switch
            {
                "OPEN" => DoorCommand.Open,
                "CLOSE" => DoorCommand.Close,
                "" or null => DoorCommand.Exit,
                _ => DoorCommand.Unknown,
            });
        }
    }
}
```

**Step 6.** 出力 Port を Console で実装します。ここが Reaction の External 実装で、`DoorCommand` に応じて `DoorState` Context を更新し、User へ結果を表示します。

`Externals/Reactions/ConsoleDoorReaction.cs`

```csharp
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Reactions;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.Externals.Reactions
{
    internal sealed class ConsoleDoorReaction : Reaction, IDoorReaction
    {
        public override void ForceResetMemoryState()
        {
        }

        public ValueTask<ReactionEnd> ReactAsync(IFlowContext context, DoorCommand command)
        {
            if (!context.TryGet<DoorState>(out var door))
            {
                Console.WriteLine("No door context.");
                return new(GetEnd());
            }

            Console.WriteLine(GetMessageAndUpdateState(door, command));
            return new(GetEnd());
        }

        private static string GetMessageAndUpdateState(DoorState door, DoorCommand command)
        {
            switch (command)
            {
                case DoorCommand.Open when !door.IsOpen:
                    door.IsOpen = true;
                    return "The door opens.";

                case DoorCommand.Open:
                    return "The door is already open.";

                case DoorCommand.Close when door.IsOpen:
                    door.IsOpen = false;
                    return "The door closes.";

                case DoorCommand.Close:
                    return "The door is already closed.";

                case DoorCommand.Exit:
                    door.ExitRequested = true;
                    return "Goodbye.";

                default:
                    return "Use Open or Close.";
            }
        }
    }
}
```

**Step 7.** Interaction を実装します。ここでは入力を取得し、その直後の `DoorCommand` を Reaction へ渡します。ドア状態の更新と表示は Reaction 側に委譲します。

`Interactions/OperateDoor.cs`

```csharp
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.OperationPorts;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.Interactions
{
    internal sealed class OperateDoor(
        IDoorOperation operation,
        IDoorReaction reaction,
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort)
        : Interaction(exceptionPort, cancellationPort, operation, reaction)
    {
        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            var command = await operation.ReadCommandAsync(context);
            return await reaction.ReactAsync(context, command);
        }
    }
}
```

**Step 8.** SystemFlow を実装します。`OperateDoor` を繰り返し、Context に終了要求が出るまで Context Loop を継続します。

`SystemFlows/DoorSystemFlow.cs`

```csharp
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.Interactions;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.SystemFlows
{
    internal sealed class DoorSystemFlow(OperateDoor operateDoor)
        : SystemFlow<IFlowContext>(operateDoor)
    {
        protected override async Task<FlowEndToken> ExecuteCoreAsync(IFlowContext context)
        {
            FlowEndToken end;

            while (true)
            {
                end = await operateDoor.ExecuteAsync(context);

                if (context.TryGet<DoorState>(out var door) &&
                    door.ExitRequested)
                {
                    break;
                }
            }

            return end;
        }
    }
}
```

**Step 9.** Program で DI、初期 Context、SystemFlow 実行を組み立てます。`OperateDoor` は独自の `IDoorOperation` / `IDoorReaction` だけでなく、`Interaction` 基底クラスが例外やキャンセルを Reaction として扱うための `IExceptionPort<Exception>` / `ICancellationPort` も必要とします。`ConsoleBuilder.Profile` は、この例外/キャンセル表示 Port の Console 実装を登録するために適用します。

`Program.cs`

```csharp
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.OperationPorts;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.ReactionPorts;
using InteractionFlow.Samples.HelloDoor.Externals.Operations;
using InteractionFlow.Samples.HelloDoor.Externals.Reactions;
using InteractionFlow.Samples.HelloDoor.Interactions;
using InteractionFlow.Samples.HelloDoor.SystemFlows;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.Console.Builders;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor
{
    internal static class Program
    {
        private static async Task Main()
        {
            var builder = new ScopeBuilder();

            // Port と External 実装、Interaction を DI に登録します。
            builder
                // Interaction の基底クラスが利用する例外/キャンセル表示 Port も登録します。
                .Apply(ConsoleBuilder.Profile)
                .UseFunction<IDoorOperation, ConsoleDoorOperation>()
                .UseFunction<IDoorReaction, ConsoleDoorReaction>()
                .UseInteraction<OperateDoor>();

            using var scope = builder.BuildScope();
            using var flow = scope.BuildSystemFlow<DoorSystemFlow, IFlowContext>();

            using var context = new ScopedFlowContext(new FlowContext())
                .With(new DoorState { IsOpen = false });

            await flow.ExecuteAsync(context);
        }
    }
}
```

**結果：**
```text
Door command (Open/Close, Enter to exit): Open
The door opens.
Door command (Open/Close, Enter to exit): Open
The door is already open.
Door command (Open/Close, Enter to exit): Close
The door closes.
Door command (Open/Close, Enter to exit): Close
The door is already closed.
Door command (Open/Close, Enter to exit):
Goodbye.
```

この分割では、`Entities` が Context に載る値、`ExternalPorts` が Interaction から見える外部依存の契約、`Externals` が Console 入出力と DoorState 更新の具体処理、`Interactions` が入力取得から Reaction 呼び出しまでの流れ、`SystemFlows` が Interaction の継続実行、`Program` が DI と初期 Context の組み立てを担当します。`DoorSystemFlow` は `while (true)` で `OperateDoor` を繰り返し、空入力で終了要求が出るまで Context Loop を継続します。

## 次のステップ (Next Step)

学習は次の順序がおすすめです。

1. [コアコンセプト](#core-concept) の概要図で全体構造を読む。
2. [Context Loop](#context-loop) の図で Runtime の循環を読む。
3. [AIとの協調](#ai-collaboration) の依存関係図で Development 側の境界を読む。
4. [docs/RoleOfMainProjects.md](./docs/RoleOfMainProjects.md) で Core / Standard / Samples の役割を確認する。
5. [docs/SystemFlowBuilder.md](./docs/SystemFlowBuilder.md) で Builder と DI スコープを理解する。
6. `InteractionFlow.Samples.HelloDoor` を実行して最小の Context Loop を見る。
7. `InteractionFlow.Samples.Parrot` を実行して Console Port と Storage の組み立てを見る。
8. `InteractionFlow.Samples.Notepad.Core` を読んで、複数 Interaction と Storage を含む構成を見る。

---

<a id="examples"></a>

# サンプル (Examples)

このリポジトリの `Examples` は、実際に存在する `InteractionFlow.Samples.*` プロジェクトに対応しています。

## InteractionFlow.Samples.HelloDoor

`HelloDoor` は、最小構成の Context Loop を確認するためのサンプルです。

- 目的: Context によって同じ Interaction の結果が変わることを確認する
- Context: ドアが開いているか、閉まっているか、終了要求があるか
- Operation: `Open` / `Close` のキーワード入力
- Interaction: `OperateDoor`
- SystemFlow: `DoorSystemFlow`
- 見どころ: 独自 Port / Console External / Interaction / SystemFlow / Program の最小分割

最初に読むサンプルとして位置づけています。詳細な実装手順は [Hello Door](#hello-door-) にあります。

## InteractionFlow.Samples.Parrot

`Parrot` は、Console 標準実装と複数 SystemFlow の組み立てを確認するためのサンプルです。

- 目的: サンプル選択から実行までの会話型フローを確認する
- Context: 選択状態、キャンセル状態、前回選択
- SystemFlow: `InitializeApplication`、`SelectAndRunSample`
- Interaction: `ListSamples`、`SelectSample`、`RunSample`、`ConsoleSetup`
- 見どころ: `ScopedFlowContext`、Memory Storage、Console Profile、依存ツリー表示

`HelloDoor` の次に読むと、Context Loop が複数 Interaction に広がる様子を追いやすくなります。

## InteractionFlow.Samples.Notepad.Core

`Notepad.Core` は、Notepad サンプルの中核となる Core プロジェクトです。

- 目的: Entity / Port / Interaction / SystemFlow の分割を確認する
- Context: `NotepadContext`、`NotepadUserObject`
- Storage: `INotepadDataStoragePort`、`INotepadUserDataStoragePort`
- Interaction: `Login`、`NoteCreate`、`NoteDelete`、`NoteEdit`、`NoteListView`、`SelectUserAction`
- SystemFlow: `MainLoop`
- 見どころ: アプリケーションの中心ルールを実行プロジェクトから分離する構成

実行環境に依存しにくい Interaction Flow を読みたい場合は、このプロジェクトから確認します。

## InteractionFlow.Samples.Notepad

`Notepad` は、`Notepad.Core` を Console アプリとして組み立てる実行サンプルです。

- 目的: Core の Port に標準的な FileSystem / Serialization / Console 実装を接続する
- Context: `NotepadContext`
- Storage: ファイル永続化、ユーザーデータ、ノートデータ
- Program: `ScopeBuilder` で Storage、Serializer、Interaction、`MainLoop` を組み立てる
- 見どころ: Core の設計を具体的な実行環境へ接続する境界

Storage を含む実用寄りの Context Loop を見たい場合に適しています。

## InteractionFlow.Samples.Notepad.Secure

`Notepad.Secure` は、`Notepad` の実行構成を差し替え、セキュアな保存とログインを追加するサンプルです。

- 目的: Core の Interaction Flow を保ったまま、Storage / Serializer / Login を差し替える
- Context: `NotepadContext` と現在ユーザーのセキュア情報
- Storage: `ICurrentUserStoragePort`、ユーザーデータ永続化、暗号化されたノートデータ
- Interaction: `EnterPassword`、`LoginSecure`
- 見どころ: Port / External の境界によって、既存 Flow を拡張できること

差し替え可能な Port 設計と、AI や開発者が読むべき Meta Context を小さく保つ効果を確認できます。

---

<a id="roadmap"></a>

# ロードマップ (Roadmap)

Interaction Flow Architecture は、次の方向で発展させる予定です。

- Core API の安定化
- Standard API の拡充
- Analyzer による依存関係ルール検査の強化
- Unity 向け API セットの開発

---

<a id="references"></a>

# 補足資料 (References)

- [Core / Standard / Samples の役割](./docs/RoleOfMainProjects.md)
- [SystemFlow Builder の詳細](./docs/SystemFlowBuilder.md)
- [計算モデルとしての Interaction Flow アーキテクチャ](./docs/ComputationalModel.md)

---

<a id="open-questions"></a>

# 確認が必要な点 (Open Questions)

後から編集しやすいように、意図が不明または現在のリポジトリとテンプレートの間に差がある点をここにまとめます。

- Installation は NuGet 利用を前提に書いています。公開先、推奨バージョン、未公開パッケージの扱いが変わる場合は調整が必要です。
- Roadmap は既存資料とテンプレートから整理した案です。リリース予定や優先順位として確定しているものではありません。

---

## 目次 (Table of Contents)

[Vision](#vision) | [Runtime](#runtime) | [Development](#development) | [Runtime × Development](#runtime-development) | [Philosophy](#philosophy) | [Packages](#packages) | [Getting Started](#getting-started) | [Examples](#examples) | [Roadmap](#roadmap) | [References](#references) | [Open Questions](#open-questions)
