# Interaction Flow Architecture

<p align="center">
  <img
    src="./docs/icon/icon_flat.svg"
    alt="Interaction Flow Architecture icon"
    width="128"
  >
</p>

<p align="center">
  <strong>Interaction shapes Context, and Context shapes Interaction.</strong>
</p>

Interaction Flow Architecture は、サービスを「コードの配置」だけではなく、User と System の間に続く Interaction と Context の循環として設計するためのアーキテクチャです。

このリポジトリは、その考え方を C# / .NET で実装するためのベースライブラリ、標準実装、Analyzer、サンプルを提供します。

## 目次 (Table of Contents)

- [ビジョン (Vision)](#vision)
- [ランタイム (Runtime)](#runtime)
- [開発 (Development)](#development)
- [ランタイム × 開発 (Runtime × Development)](#runtime-development)
- [哲学 (Philosophy)](#philosophy)
- [パッケージ (Packages)](#packages)
- [はじめに (Getting Started)](#getting-started)
- [サンプル (Examples)](#examples)
- [ロードマップ (Roadmap)](#roadmap)
- [補足資料 (References)](#references)
- [確認が必要な点 (Open Questions)](#open-questions)

---

<a id="vision"></a>

# ビジョン (Vision)

## なぜ新しいアーキテクチャなのか？ (Why a New Architecture?)

レイヤードアーキテクチャやクリーンアーキテクチャは、コードの依存方向や責務分離を整理する強力な考え方です。一方で、対話的なサービス、ゲームループ、エージェント、複雑な入力と出力を持つアプリケーションでは、開発者が本当に設計したいものは「どのクラスをどこに置くか」だけではありません。

設計したいものは、User が何を行い、System がどう受け取り、何を返し、その結果として次の Interaction がどう変わるかです。

Interaction Flow Architecture は、この流れを `Interaction` と `Context` を中心に表現します。Runtime では User がサービスを体験し、Development では開発者が同じ構造をコードとして設計し、AI はその構造を読み取って補助します。三者が別々の説明を持つのではなく、一つの共有モデルを使うことを目指します。

## コアコンセプト (Core Concept)

> Interaction shapes Context, and Context shapes Interaction.

Interaction は Context を更新します。更新された Context は、次に選ばれる Interaction、表示される Reaction、必要になる Operation、保存される Storage を変えます。

この循環が最小単位です。

```text
Interaction -> Context -> next Interaction -> next Context -> ...
```

この README では、次の言葉を共通言語として使います。

- `User`: System と相互作用する主体。人間だけでなく、AI エージェント、ロボット、他システムも含みます。
- `SystemFlow`: System 側が User への反応プロセスとして Interaction を束ねる単位。
- `Interaction`: システム内部の目的を達成する意味単位。
- `Context`: 現在の SystemFlow に関する状態、状況、文脈的情報。
- `Operation`: User からの入力や外部条件の取得。
- `Reaction`: User が観測できる出力や終了時の反応。
- `Storage`: 永続または一時の状態管理。
- `External`: UI、DB、ファイルシステム、OS、外部サービスなど、具体的な外部依存。

![Interaction Flow Architecture overview](./docs/img/InteractionFlowArchitecture_Overview.svg)

代替テキスト: [InteractionFlowArchitecture_Overview.context.md](./docs/img/InteractionFlowArchitecture_Overview.context.md)

---

<a id="runtime"></a>

# ランタイム (Runtime)

> ユーザーがサービスとどのように関わるか。

## ユーザー体験 (User Experience)

User から見たサービスは、画面や API エンドポイントの集合ではなく、Interaction と Context の連続です。

たとえば、同じボタンを押しても、ログイン前とログイン後では意味が変わります。同じ入力でも、前の会話、保存済みデータ、権限、現在の選択状態によって System の反応は変わります。つまり、体験は「操作そのもの」ではなく「Context の中で解釈された Interaction」です。

Interaction Flow Architecture は、この体験の連続性を Runtime の中心に置きます。

## 最初の Interaction (First Interaction)

🚪 ドアを開閉する (Open / Close the Door)

最小の Context Loop は、ドアの例で考えると直感的です。

```text
1. User が Open または Close を入力する
2. System は現在の Context を見る
3. ドアが閉まっていて Open なら、ドアを開ける
4. ドアが開いていて Close なら、ドアを閉める
5. すでに同じ状態なら、その状態を Reaction として返す
```

ここで重要なのは、Interaction が単独で意味を持つのではないことです。同じ `Open` という入力でも、Context が「閉じている」なら開けられ、「開いている」ならすでに開いているという Reaction になります。そして、その結果として更新された Context が、次の Interaction を形作ります。

## Context Loop

Context Loop は、SystemFlow の実行によって Context が更新され、その更新済み Context が次回以降に再利用される循環です。

![Interaction Flow Architecture flow diagram](./docs/img/InteractionFlowArchitecture_FlowDiagram.svg)

代替テキスト: [InteractionFlowArchitecture_FlowDiagram.context.md](./docs/img/InteractionFlowArchitecture_FlowDiagram.context.md)

図の Runtime は、次の流れとして読めます。

- `Program` がエントリーポイント、イベント、リクエストを受け取る。
- `Program` が `Context` を作成または再利用する。
- `SystemFlow Builder Block` が `SystemFlow` と依存オブジェクトを構築する。
- `SystemFlow` が `Interaction` を実行する。
- `Interaction` が Port を通じて Operation / Reaction / Storage / SilentExternal を利用する。
- 実行結果として `Context` が更新される。
- 更新された `Context` が次の Interaction に引き継がれる。

サービス全体は、単一の巨大なフローではなく、複数の Context Loop の集合として設計できます。

## ランタイムアーキテクチャ (Runtime Architecture)

Runtime の責務は、次の要素に分けられます。

- `Interaction`: System 内部の目的を達成するために Function Port をオーケストレーションする。
- `Context`: 現在の SystemFlow に関する状態、状況、文脈情報を渡し、必要に応じて更新する。
- `Operation`: User 入力や外部条件を取得する。
- `Reaction`: User に観測可能な出力、完了、キャンセル、例外処理を返す。
- `Storage`: DB、ファイルシステム、設定、メモリなどへの状態保存と取得を扱う。
- `External`: OS、Framework、UI、DB、外部サービスなどの具体的な実装環境を扱う。

実行フローは下に進みます。

```text
SystemFlow -> Interaction -> Function Port -> Function External -> External
```

一方で、外部依存の詳細は `Function Port` の境界で分離されます。Interaction は UI や DB を直接知りません。Interaction は Port を呼び、External が Port を実装します。

### 実行時の振る舞い

`Operation`、`Storage`、`Reaction`、`SilentExternal` は、必要に応じて中断、例外、キャンセル、mutable な状態を扱います。

一方で、`SystemFlow` と `Interaction` は中断を外へ投げっぱなしにする単位ではありません。例外やキャンセルを Reaction に変換し、User に観測可能な終了として扱います。また、SystemFlow / Interaction の実行中に一時的な遷移状態を持つことはありますが、フローのスコープ終了時に破棄されるものとして設計します。

## ユーザー体験を設計する (Designing User Experience)

Interaction Flow を設計することは、ユーザー体験を設計することです。

UI を中心に考えると、「どの画面に何を置くか」が先に立ちます。Interaction を中心に考えると、「User はどの Context で何を行い、System はどの Context を返すべきか」が先に立ちます。

この順序にすると、画面、CLI、API、AI エージェント、ゲームループなどの入出力形態が変わっても、体験の意味を保ちやすくなります。

---

<a id="development"></a>

# 開発 (Development)

> 開発者がサービスとどのように関わるか。

## 開発体験 (Development Experience)

開発者も Runtime と同じ Context を扱っています。

コードを書くことは、単に処理を並べることではありません。どの Context を受け取り、どの Interaction を実行し、どの Port を通じて外部に触れ、どの Reaction で終えるかを定義することです。

このアーキテクチャでは、開発者の判断が次の問いに集約されます。

- この目的は `SystemFlow` か、`Interaction` か。
- この値は `Context` か、Domain の Entity か、Storage に保存される Data か。
- この外部依存は Operation / Reaction / Storage / SilentExternal のどれか。
- この依存は Port と External の境界を越えていないか。

## Interaction Flow を設計する (Designing Interaction Flow)

Interaction は、システム内部における単一の意味を持つ処理単位として設計します。

大きすぎる Interaction は、Context 更新の理由が読みにくくなります。小さすぎる Interaction は、システム内での意味を失い、単なる関数分割になります。目安は「User から見えない内部目的として、名前を付けられるか」です。

SystemFlow は、User への反応プロセスとして Interaction を束ねます。SystemFlow は全システムの処理手順ではなく、User と System の関係として一つの意味を持つ単位です。

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

- `Operation` は、User 入力や外部条件を読み取る。
- `Reaction` は、User へ結果を返し、終了状態を表す。
- `Storage` は、永続または一時の状態を保存、復元する。
- `SilentExternal` は、User に直接見えない外部状態や外部イベントを扱う。
- `Interaction` は、これらの Port を組み合わせて Context の意味ある更新を構成する。
- `SystemFlow` は、Interaction の順序と終了の意味を構成する。

この分担により、Context 更新の理由が Interaction Flow 上に残ります。

## デザイン言語としてのコード (Code as a Design Language)

コードは、Architecture の説明そのものです。

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

ドキュメントも Context の一部です。

README は、プロジェクト全体の共有 Context です。図は、構造を短時間で復元するための圧縮 Context です。コメントは、コードだけでは読み取れない設計判断を残す Context です。

特に図を更新する場合は、対応する `.context.md` と `.svg` も更新し、AI と人間の両方が同じ意味を参照できるようにします。

### Analyzer

Analyzer は、人間の注意力だけに依存せず、アーキテクチャのルールを検査するための仕組みです。

依存関係の規約、Layer / Block の境界、Port と External の分離は、レビューだけで守るには忘れやすいものです。Analyzer によって設計ルールを開発環境に近づけることで、Architecture を「読んで守るもの」から「書きながら支援されるもの」にします。

## AIとの協調 (AI Collaboration)

AI は Context を読み取る存在です。

Interaction Flow Architecture は、AI に渡す Context を圧縮します。ファイル構成、名前空間、図、README、Analyzer のルールが揃っていると、AI は「この変更はどの責務に属するか」「どの境界を越えてはいけないか」を短い文脈で復元できます。

人間はモデルを設計し、AI はモデルを展開します。人間は意味と境界を決め、AI はその境界の内側で実装、検証、反復を支援します。

![Interaction Flow Architecture dependency diagram](./docs/img/InteractionFlowArchitecture_DependencyDiagram.svg)

代替テキスト: [InteractionFlowArchitecture_DependencyDiagram.context.md](./docs/img/InteractionFlowArchitecture_DependencyDiagram.context.md)

---

<a id="runtime-development"></a>

# ランタイム × 開発 (Runtime × Development)

## 二つの視点 (Two Perspectives)

Runtime と Development は、同じ構造を別の向きから見ています。

Runtime では、User が Operation を行い、Reaction を受け取り、Context が更新されます。Development では、開発者が SystemFlow、Interaction、Port、External、Domain を設計し、その Context Loop を実装します。

見えているものは違いますが、扱っているモデルは同じです。

## 一つの共有モデル (One Shared Model)

共有する概念は二つです。

- `Interaction Flow`
- `Context`

Users experience it.
Developers design it.
AI learns it.

User は Interaction Flow を体験します。開発者は Interaction Flow を設計します。AI は Interaction Flow を学習し、補助します。

この三者が同じモデルを共有できることが、このアーキテクチャの実用上の価値です。

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

## Interaction

Interaction は、User と System の間に起きる作用であり、System 内部では目的を持った状態遷移のまとまりです。

Interaction は単なる関数呼び出しではありません。Operation を読み、Storage を参照し、必要な外部状態に触れ、Reaction を返し、Context を次へ進めます。

このアーキテクチャが Interaction を中心に置くのは、サービスの体験が Interaction の連続として現れるからです。

## Context

Context は、現在の SystemFlow に関する状態、状況、文脈的情報です。

Context は state と似ていますが、意味が少し違います。state は値そのものに注目します。Context は、その値が次の Interaction をどう変えるかに注目します。

たとえば「ログイン済み」という値は state です。その値によって「ノート一覧を表示できる」「編集できる」「ログイン画面に戻す」といった次の Interaction が決まるとき、それは Context として働きます。

## 共有Context (Shared Context)

人、AI、サービスは、それぞれ違う形で Context を扱います。

User は体験として Context を受け取ります。開発者はコードとドキュメントとして Context を設計します。AI はファイル、図、命名、型、コメントから Context を読み取ります。

共有 Context が明確であるほど、協調は楽になります。説明が短くなり、誤解が減り、変更の影響範囲が見えやすくなります。

## アーキテクチャは共有Contextである (Architecture as Shared Context)

Architecture は、コードのルール集だけではありません。

Architecture は、チーム、AI、User をつなぐ共有 Context です。どこに何を書くか、どの責務を混ぜないか、どの言葉で設計を語るかを揃えることで、サービスの体験と実装の構造が同じ方向を向きます。

Interaction Flow Architecture は、User の体験、開発者の実装、AI の理解を、Interaction と Context という一つのモデルに集約します。

## 計算モデルとしての補助線 (Computational Model)

Interaction Flow Architecture は、チューリングマシンとしても説明できます。

この見方では、Interaction は状態遷移、Operation / Reaction / Storage / SilentExternal は「読み取り、書き込み、外部への作用」を担うテープ操作に相当します。これはアーキテクチャの出発点を置き換えるものではなく、Interaction と Context の循環を計算モデルとして検証しやすくするための補助線です。

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

このリポジトリには、現在次のサンプルがあります。

- `InteractionFlow.Samples.Parrot`: コンソールベースの基本サンプル。SystemFlow、Interaction、Console Port、Storage の組み立てを確認できます。
- `InteractionFlow.Samples.HelloDoor`: `Open` / `Close` のキーワード入力でドアの Context Loop を確認する最小サンプルです。
- `InteractionFlow.Samples.Notepad.Core`: Notepad サンプルの中核。Entity / Port / Interaction / SystemFlow をまとめます。
- `InteractionFlow.Samples.Notepad`: Notepad サンプルの実行プロジェクト。Core のフローをコンソールアプリとして組み立てます。
- `InteractionFlow.Samples.Notepad.Secure`: Notepad サンプルの拡張。パスワードベースの暗号化や安全なユーザーデータ管理を扱います。
- `InteractionFlow.PackageInstallCheck`: パッケージ導入確認用のプロジェクトです。

学習順序は、`HelloDoor` で最小の Context Loop を見てから、`Parrot` で Console Port と Storage の組み立てを確認し、`Notepad.Core` で複数 Interaction と Storage を含む構成を読むのがおすすめです。

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

Hello Door は、最初の Context Loop を理解するための最小例です。

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

C# では、Operation と Reaction を Console 外部依存として実装し、明示的な `Program` クラスから SystemFlow を実行します。実装は `InteractionFlow.Samples.HelloDoor` にあり、README のコードはファイル単位で同じ責務に対応しています。

実行する場合は、次のコマンドを使います。

```bash
dotnet run --project InteractionFlow.Samples.HelloDoor/InteractionFlow.Samples.HelloDoor.csproj
```

Step 1. User 入力を Interaction が扱うコマンドとして定義します。

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

Step 2. Context に載せるドアの状態を定義します。`IsOpen` は現在の開閉状態、`ExitRequested` は SystemFlow のループ終了要求です。

`Entities/DoorState.cs`

```csharp
namespace InteractionFlow.Samples.HelloDoor.Entities
{
    internal sealed class DoorState
    {
        // Interaction が読み書きする現在のドア状態です。
        public bool IsOpen { get; set; }

        // 空入力を受けたときに SystemFlow のループを止めるためのフラグです。
        public bool ExitRequested { get; set; }
    }
}
```

Step 3. Interaction から見える入力 Port を定義します。Interaction は Console を直接読まず、この Port だけを呼びます。

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
        // User 入力を DoorCommand へ変換して返します。
        ValueTask<DoorCommand> ReadCommandAsync(IFlowContext context);
    }
}
```

Step 4. Interaction から見える出力 Port を定義します。Interaction は Console を直接書かず、この Port だけを呼びます。

`ExternalPorts/ReactionPorts/IDoorReaction.cs`

```csharp
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.ExternalPorts.ReactionPorts
{
    internal interface IDoorReaction : IReactionPort
    {
        // User に観測できる結果を返します。
        ValueTask<ReactionEnd> WriteAsync(IFlowContext context, string message);
    }
}
```

Step 5. 入力 Port を Console で実装します。ここが外部依存の実体です。

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

            // Console 入力を Interaction が扱いやすいコマンドへ変換します。
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

Step 6. 出力 Port を Console で実装します。`ReactionEnd` を返すことで、Interaction の終了結果になります。

`Externals/Reactions/ConsoleDoorReaction.cs`

```csharp
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Reactions;
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

        public ValueTask<ReactionEnd> WriteAsync(IFlowContext context, string message)
        {
            Console.WriteLine(message);
            // Reaction が正常に完了したことを Interaction へ返します。
            return new(GetEnd());
        }
    }
}
```

Step 7. Interaction を実装します。ここで Command と Context を見て、ドアを開けるか閉めるかを決めます。

`Interactions/OperateDoor.cs`

```csharp
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.HelloDoor.Entities;
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
            if (!context.TryGet<DoorState>(out var door))
            {
                return await reaction.WriteAsync(context, "No door context.");
            }

            // 入力は Port から取得します。Console 実装かどうかは Interaction からは見えません。
            var command = await operation.ReadCommandAsync(context);

            return command switch
            {
                DoorCommand.Open when !door.IsOpen => await OpenAsync(),
                DoorCommand.Open => await reaction.WriteAsync(context, "The door is already open."),
                DoorCommand.Close when door.IsOpen => await CloseAsync(),
                DoorCommand.Close => await reaction.WriteAsync(context, "The door is already closed."),
                DoorCommand.Exit => await ExitAsync(),
                _ => await reaction.WriteAsync(context, "Use Open or Close."),
            };

            async Task<ReactionEnd> OpenAsync()
            {
                // Context を更新し、次の Interaction から見える状態を変えます。
                door.IsOpen = true;
                return await reaction.WriteAsync(context, "The door opens.");
            }

            async Task<ReactionEnd> CloseAsync()
            {
                // Context を更新し、次の Interaction から見える状態を変えます。
                door.IsOpen = false;
                return await reaction.WriteAsync(context, "The door closes.");
            }

            async Task<ReactionEnd> ExitAsync()
            {
                // SystemFlow のループ終了条件を Context に残します。
                door.ExitRequested = true;
                return await reaction.WriteAsync(context, "Goodbye.");
            }
        }
    }
}
```

Step 8. SystemFlow を実装します。`OperateDoor` を繰り返し、Context に終了要求が出るまで Context Loop を継続します。

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

                // 終了判断も Context を通じて行います。
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

Step 9. Program で DI、初期 Context、SystemFlow 実行を組み立てます。

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
                .Apply(ConsoleBuilder.ProfileUseCancellation)
                .UseFunction<IDoorOperation, ConsoleDoorOperation>()
                .UseFunction<IDoorReaction, ConsoleDoorReaction>()
                .UseInteraction<OperateDoor>();

            using var scope = builder.BuildScope();
            using var flow = scope.BuildSystemFlow<DoorSystemFlow, IFlowContext>();

            // 最初の Context は「ドアが閉じている」状態から始めます。
            using var context = new ScopedFlowContext(new FlowContext())
                .With(new DoorState { IsOpen = false });

            await flow.ExecuteAsync(context);
        }
    }
}
```

この分割では、`Entities` が Context に載る値、`ExternalPorts` が Interaction から見える外部依存の契約、`Externals` が Console 実装、`Interactions` が Context 更新の判断、`SystemFlows` が Interaction の継続実行、`Program` が DI と初期 Context の組み立てを担当します。`DoorSystemFlow` は `while (true)` で `OperateDoor` を繰り返し、空入力で終了要求が出るまで Context Loop を継続します。

## 次のステップ (Next Step)

学習は次の順序がおすすめです。

1. [docs/img/InteractionFlowArchitecture_Overview.context.md](./docs/img/InteractionFlowArchitecture_Overview.context.md) で全体構造を読む。
2. [docs/img/InteractionFlowArchitecture_FlowDiagram.context.md](./docs/img/InteractionFlowArchitecture_FlowDiagram.context.md) で Runtime の Context Loop を読む。
3. [docs/img/InteractionFlowArchitecture_DependencyDiagram.context.md](./docs/img/InteractionFlowArchitecture_DependencyDiagram.context.md) で依存関係を読む。
4. [docs/RoleOfMainProjects.md](./docs/RoleOfMainProjects.md) で Core / Standard / Samples の役割を確認する。
5. [docs/SystemFlowBuilder.md](./docs/SystemFlowBuilder.md) で Builder と DI スコープを理解する。
6. `InteractionFlow.Samples.HelloDoor` を実行して最小の Context Loop を見る。
7. `InteractionFlow.Samples.Parrot` を実行して Console Port と Storage の組み立てを見る。
8. `InteractionFlow.Samples.Notepad.Core` を読んで、複数 Interaction と Storage を含む構成を見る。

---

<a id="examples"></a>

# サンプル (Examples)

## Door

Door は、最小構成の Interaction Flow を説明するための概念サンプルです。

- 目的: Context によって同じ Interaction の結果が変わることを示す
- Context: ドアが開いているか、閉まっているか
- Operation: `Open` / `Close` のキーワード入力
- Interaction: OperateDoor
- Reaction: 開いた、閉じた、すでに開いている、すでに閉じている

実装は `InteractionFlow.Samples.HelloDoor` に含まれています。

## Counter

Counter は、状態更新を伴う Context Loop を説明するための概念サンプルです。

- 目的: Interaction が Context を更新し、次の表示や入力可能範囲が変わることを示す
- Context: 現在値、上限、下限
- Interaction: Increment / Decrement / Reset
- Storage: 必要に応じて現在値を保存する

現在のリポジトリには専用プロジェクトとしては含まれていません。

## Inventory

Inventory は、Storage を利用した Interaction Flow の例として位置づけられます。

- 目的: Context と永続化されたデータの境界を示す
- Context: 現在選択中のアイテム、操作中のユーザー、表示条件
- Storage: アイテム一覧、在庫数、変更履歴
- Interaction: AddItem / RemoveItem / ListItems

現在のリポジトリでは、Storage を含む実例として `InteractionFlow.Samples.Notepad.Core` が近い役割を持っています。

## Dialogue

Dialogue は、複数の Interaction が連続する会話型フローの例です。

- 目的: Reaction が次の Operation を誘導し、Context Loop が会話を進めることを示す
- Context: 会話の現在地、選択済み項目、キャンセル状態
- Interaction: Prompt / Select / Confirm / Execute

現在のリポジトリでは、`InteractionFlow.Samples.Parrot` の `SelectAndRunSample` と `InteractionFlow.Samples.Notepad.Core` の `MainLoop` が、複数 Interaction を束ねる実例です。

---

<a id="roadmap"></a>

# ロードマップ (Roadmap)

Interaction Flow Architecture は、次の方向で発展させる予定です。

- Core API の安定化と、Context / EndToken / Port 境界の整理
- Standard の実用 API 拡充
- Analyzer による依存関係ルール検査の強化
- Counter / Inventory / Dialogue など、学習順に沿った小さなサンプルの追加
- 図、`.context.md`、README を同期した AI 参照向けドキュメント整備
- Runtime と Development を一つのモデルとして説明する設計資料の拡充

---

<a id="references"></a>

# 補足資料 (References)

- [Core / Standard / Samples の役割](./docs/RoleOfMainProjects.md)
- [SystemFlow Builder の詳細](./docs/SystemFlowBuilder.md)
- [計算モデルとしての Interaction Flow アーキテクチャ](./docs/ComputationalModel.md)
- [図の確認用ビューア](./docs/img/ImageViewer.md)
- [AGENTS.md](./AGENTS.md)

---

<a id="open-questions"></a>

# 確認が必要な点 (Open Questions)

後から編集しやすいように、意図が不明または現在のリポジトリとテンプレートの間に差がある点をここにまとめます。

- Counter / Inventory / Dialogue はテンプレート上の章として含めましたが、現在のリポジトリには専用サンプルプロジェクトがありません。正式サンプルとして追加予定か、概念例として維持するか確認が必要です。
- Installation は NuGet 利用を前提に書いています。公開先、推奨バージョン、未公開パッケージの扱いが変わる場合は調整が必要です。
- Roadmap は既存資料とテンプレートから整理した案です。リリース予定や優先順位として確定しているものではありません。

---

## 目次 (Table of Contents)

[Vision](#vision) | [Runtime](#runtime) | [Development](#development) | [Runtime × Development](#runtime-development) | [Philosophy](#philosophy) | [Packages](#packages) | [Getting Started](#getting-started) | [Examples](#examples) | [Roadmap](#roadmap) | [References](#references) | [Open Questions](#open-questions)
