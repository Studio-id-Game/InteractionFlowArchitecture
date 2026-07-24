# Interaction Flow Architecture　<a id="top"></a>

<p align="center">
    <img
        src="./docs/icon/icon_flat.svg"
        alt="Interaction Flow Architecture icon"
        width="128"
    >
</p>

<p align="center">
    <i>
        Interaction shapes Context, <br>
        and Context shapes Interaction.
    </i>
</p>

<br>

<p align="center">
    このアーキテクチャは、<br/>
    開発対象となるシステムを「コードやレイヤーの塊」として見るだけではなく、<br/>
    <b>「ユーザーとシステムの境界で生まれる相互作用」と「循環する文脈」</b> をコード表現に落とし込み、<br/>
    ユーザー体験を直接設計するためのアーキテクチャです。
</p>

## 目次

- [パッケージとインストール](#packages)
- [ビジョン](#vision)
- [Context Loop & System Flow](#context-loop-system-flow)
- [はじめに](#getting-started)
- [サンプル解説](#examples)
- [Philosophy](#philosophy)
- [ライブラリの実装](#implementation)
- [計算モデルとしての補助線](#computational-model)
- [ロードマップ](#roadmap)
- [資料まとめ](#references)

---

# パッケージとインストール <a id="packages"></a>
> Interaction Flow for C#

このリポジトリは、Interaction Flow Architecture を C# / .NET で実装するための、
ベースライブラリやAnalyzerのパッケージ、サンプルプログラム等を提供しています。

まず使い始めたい場合はこの章や [はじめに](#getting-started) から、設計やコンセプトを理解したい場合は [ビジョン](#vision) から読み進めてください。

`Core`、`Standard`、`Samples` の詳細な責務と更新方針は [.Core/.Standard/.Samples それぞれの役割](./docs/RoleOfMainProjects.md) を参照してください。

**もっとも標準的なインストール:**
```xml
<ItemGroup>
  <PackageReference Include="InteractionFlow.Standard" Version="0.5.0" />
  <PackageReference Include="InteractionFlow.Analyzers" Version="0.5.0" PrivateAssets="all" />
</ItemGroup>
```

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
  
**より強力なアーキテクチャ支援を受けるために導入を推奨します。**<br>
（ビルドに含まれないように、プロジェクトファイルから `PrivateAssets="all"` を付ける事を推奨）


## Samples
- `InteractionFlow.Samples.*` は、Architecture の使い方を具体例として確認するためのプロジェクト群です。
- 個別サンプルの目的と読み方は [サンプル解説](#examples) にまとめています。
- パッケージとしての公開はありません。ソリューションからビルドして実行してください。
  ```bash
  # Build InteractionFlow
  dotnet build InteractionFlow.slnx

  # Run InteractionFlow.Samples.*
  dotnet run --project InteractionFlow.Samples.HelloDoor
  dotnet run --project InteractionFlow.Samples.Parrot
  dotnet run --project InteractionFlow.Samples.Notepad
  dotnet run --project InteractionFlow.Samples.Notepad.Secure
  ```

## Others

- `InteractionFlow.PackageInstallCheck` は、リポジトリ運用のための補助プロジェクトです。

---

# ビジョン <a id="vision"></a>

## なぜ、新しいアーキテクチャが必要なのか？

> <I>「ユーザー体験」をクリーンに保つための制約を持ったアーキテクチャ。</I>

レイヤードアーキテクチャやクリーンアーキテクチャは、コードの依存方向や責務分離を整理する強力な考え方です。一方で、これらのアーキテクチャモデルは、モデル上の要素と実際のコードとの対応関係を強くは定めません。

そのため、設計上の境界とコード上の境界の一致を継続的に保証することは難しく、両者の間に乖離が生じる可能性があります。また、同じ名称や図を共有していても、開発者ごとに異なる対応関係を想定できるため、解釈の余地が設計の自由度としてだけでなく、認識の曖昧さとしても現れます。

特に、対話的な UI、ゲームループ、エージェントなど、複雑な入力と出力を持つシステムでは、処理が複数のレイヤーを横断しながら、継続的に状態を更新し、次の振る舞いを形成します。そのため、アーキテクチャモデル上の責務分割だけでは、実際のコードがどのような実行フローを形成するのかを十分に表現できず、コードとモデルの対応関係はさらに曖昧になります。

また、このようなインタラクティブなシステムの開発において、開発者が本当に設計したいものは、「どのクラスをどのレイヤーに置くか」だけではありません。ユーザーが何を行い、システムがそれをどう受け取り、その結果として文脈がどう変化し、次の相互作用がどのように形づくられるかという、相互作用の流れによる「ユーザー体験（UX）」そのものです。

Interaction Flow Architecture は、レイヤー構造による責務分離を維持しながら、相互作用をコード上の基本単位として設計します。これにより、モデルとコードの対応関係を明確に保ちつつ、ユーザーとシステムの間に生まれる相互作用の流れ＝「ユーザー体験」を、そのままコードとして記述できます。

このアーキテクチャでは、コードを実行した結果としてユーザー体験が生まれるのではなく、コードそのものがユーザー体験の設計言語になります。

Interaction Flow Architecture は、単なるコードの整理規則ではありません。**「ユーザー体験」をクリーンに保つための制約を持ったアーキテクチャ** です。

## コアコンセプト

> <I>相互作用が文脈を形作り、文脈が相互作用を形作る。</I>

Interaction Flow Architecture では、
ユーザーとシステムの相互作用（`Interaction`）と、相互作用の状態を表す文脈（`Context`）を用いて、相互作用が文脈を更新し、新しい文脈が次の相互作用に影響する過程の繰り返し（`Context Loop`）として、システムを表現します。

```text
Context -> Interaction -> next Context -> next Interaction -> ...
```

このアーキテクチャでは、次の言葉を共通言語として使います。

### 基本概念:

| <div style="width: 110px;">名称</div> | 対象 |
| --- | --- |
| `User` |  System と相互作用する主体。人間だけでなく、ロボット、AI エージェント、動物など、様々な主体を含む。 |
| `Context` | 現在の相互作用に関する状態や状況に、次の相互作用に影響を与える文脈的な意味を持たせた情報。 |
| `System` | `User` と相互作用する開発対象。`Context` を介して `User` の行為に反応し、動作する。 |
| `Context Loop` | `System` と `User` の間にある、 `Context` を介した繰り返しの反応プロセス。 |
| `System Flow` | `Context Loop` の一環として、複数の相互作用を通じて `System` が `User` との関係を構築するための単位。|
| `Interaction` | `Context Loop` の一環として、`System` が内部の目的を達成するための相互作用の単位。|

### 実装概念:

| <div style="width: 110px;">名称</div> | 対象 |
| --- | --- |
| `Domain` | `System` の前提となる、外部に依存しないデータ構造や動作の定義。 |
| `External` | UI、DB、ファイルシステム、OS、外部サービスなど、具体的な実行環境。 |
| `Function` | `Interaction` から呼び出される機能の単位。|
| `Function Port` | `Interaction` が扱う `Function` の抽象。|
| `Function External` | `Interaction` が扱う `Function` の実装。`External` に依存出来る。|
| `Operation` | `Function` の一種。`User` が操作できる入力を受け付ける機能。 |
| `Reaction` | `Function` の一種。`User` が観測できる反応を提供する機能。 |
| `Storage` | `Function` の一種。`Context` の文脈的な意味とは独立して、データを保持する機能。 |
| `Silent External` | `Function` の一種。`User` との相互作用やデータの記録を目的とせず、外部環境と情報をやり取りする機能。 |

#### アーキテクチャの全体図:

![Interaction Flow Architecture overview](./docs/img/InteractionFlowArchitecture_Overview.svg)

代替テキスト: [Interaction Flow Architecture - Overview Context](./docs/img/InteractionFlowArchitecture_Overview.context.md)

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
2. ユーザーとのやり取りを通じて Context を更新し、
3. 次の Interaction を変化させる。

というループが、Context Loop の基本となります。

また、System Flow は、複数の Interaction をまとめることで、System における1つのユーザー体験として Context Loop を表現します。ドアの Context Loop の例は、1つの Interaction で構成される最小の System Flow であるとも言えます。

[はじめに](#getting-started) では、このモデルを実際のコードとして実装する手順を、ステップに分けて解説しています。

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

特に、(5.) でデザインする System Flow の実装は、ユーザーが体験する Context Loop そのものになります。

![Interaction Flow Architecture flow diagram](./docs/img/InteractionFlowArchitecture_FlowDiagram.svg)

代替テキスト: [Interaction Flow Architecture - Flow Diagram Context](./docs/img/InteractionFlowArchitecture_FlowDiagram.context.md)

## 共有される Context Loop & System Flow

> <I>Context Loop と System Flow によって、開発者 / System / User が同じ世界を共有する。</I>

開発者によりデザインされた System Flow と、それによって User が体験する Context Loop は、開発者 / System / User が同じ世界を共有するための共通モデルとなります。

- 開発者は、System Flow をデザインし、Context Loop として実装する
- System は、System Flow を実行し、Context Loop を提供する
- User は、System Flow の中で、Context Loop を体験する

冒頭で、インタラクティブなシステムの開発において、開発者が本当に設計したいものは「ユーザー体験」であると述べました。

ここで述べた Context Loop こそが、実行モデルとしての「ユーザー体験」であり、System Flow のデザインこそが、「ユーザー体験の設計」となります。**System Flow のデザインによって、開発者がユーザー体験を直接設計できるようになる** ことが、Interaction Flow Architecture の最大の利点です。

---

# はじめに <a id="getting-started"></a>

## インストール

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
  <PackageReference Include="InteractionFlow.Standard" Version="0.5.0" />
  <PackageReference Include="InteractionFlow.Analyzers" Version="0.5.0" PrivateAssets="all" />
</ItemGroup>
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

このサンプルでは、Operation と Reaction を Console 外部依存として実装し、`Program` クラスから SystemFlow を実行します。
実装は `InteractionFlow.Samples.HelloDoor` にあり、README のコードはファイル単位で同じ責務に対応しています。

実行する場合は、次のコマンドを使います。

```bash
dotnet build InteractionFlow.slnx
dotnet run --project InteractionFlow.Samples.HelloDoor
```

### 実装

#### Step 1. User 入力を Interaction が扱うコマンドとして定義します。

<details>
<summary><code>Entities/DoorCommand.cs</code> のコードを表示</summary>

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

</details>

#### Step 2. Context に載せるドアの状態を定義します。

`IsOpen` は現在の開閉状態、`ExitRequested` は SystemFlow のループ終了要求です。

<details>
<summary><code>Entities/DoorState.cs</code> のコードを表示</summary>

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

</details>

#### Step 3. Interaction から扱うユーザー操作を定義します。

ユーザー操作の `DoorCommand` への変換を担当します。

<details>
<summary><code>ExternalPorts/OperationPorts/IDoorOperation.cs</code> のコードを表示</summary>

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

</details>

#### Step 4. Interaction から扱うシステム反応を定義します。

指定された `DoorCommand` による Context への影響と結果表示を担当します。

<details>
<summary><code>ExternalPorts/ReactionPorts/IDoorReaction.cs</code> のコードを表示</summary>

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

</details>

#### Step 5. ユーザー操作 を Console で実装します。

Console 標準入力による `DoorCommand` の取得を担当します。

<details>
<summary><code>Externals/Operations/ConsoleDoorOperation.cs</code> のコードを表示</summary>

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

</details>

#### Step 6. システム反応 を Console で実装します。

`DoorCommand` に応じた `DoorState` Context の更新と、
Console 標準出力による User への結果表示を担当します。

<details>
<summary><code>Externals/Reactions/ConsoleDoorReaction.cs</code> のコードを表示</summary>

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

</details>

#### Step 7. 相互作用を実装します。

ここではユーザー操作（`IDoorOperation`）から `DoorCommand` を取得し、`IDoorReaction` へ渡します。ドア状態の更新と結果表示は Reaction 側で行われています。

<details>
<summary><code>Interactions/OperateDoor.cs</code> のコードを表示</summary>

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

</details>

#### Step 8. ユーザー体験を実装します。

`OperateDoor` を繰り返し、Context に終了要求が出るまで Context Loop を継続します。

<details>
<summary><code>SystemFlows/DoorSystemFlow.cs</code> のコードを表示</summary>

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

</details>

#### Step 9. エントリーポイントで、実行環境の組み立てと実行を実装します。

`ScopeBuilder`を用いてPort、External実装、InteractionをDIへ登録し、初期ContextとSystemFlowを構築して実行します。`OperateDoor` は、`Interaction` 基底クラスが例外やキャンセルを Reaction として扱うための実装も必要とします。この実装として Console 実装を登録するために、`ConsoleBuilder.Profile` も適用します。

<details>
<summary><code>Program.cs</code> のコードを表示</summary>

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

</details>

#### 実行結果

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

#### 責務と関心

この例では、それぞれの要素は以下のように分割された責務と関心を持っています。

| 要素 | 責務 | 関心 |
| --- | --- | --- |
| `Entities.DoorState`             | ドアの状態の表現 | ドアはどのような情報を持つか？ |
| `OperationPorts.IDoorOperation`   | ユーザーによるドアの操作の抽象化 | ユーザーはドアに対して何ができるか？ |
| `ReactionPorts.IDoorReaction`    | システムによるドアの反応の抽象化 | ドアはユーザーに対して何ができるか？ |
| `Operations.ConsoleDoorOperation` | Console によるドアの操作の実装 | ドアの操作をどのように実現できるか？ |
| `Reactions.ConsoleDoorReaction`  | Console によるドアの反応の実装 | ドアの反応をどのように実現できるか？ |
| `Interactions.OperateDoor`       | ドアとユーザーの相互作用の実装 | ドアはユーザーとどのように相互作用するか？ |
| `SystemFlows.DoorSystemFlow`                    | ドアにおけるユーザー体験の実装 | ドアはユーザーにどのように体験されるか？ |
| `Program`                        | システムの実体化<br/>Context の組み立て<br/>システムの実行 | どんなドアと、どんなユーザーを組み合わせるか？ |

このような責務と関心の分離は、コードをクリーンに保つことだけが目的ではありません。
コードのクリーンさを保ちながら、ユーザー体験と、ユーザー体験の設計をクリーンに保つための分離です。


<details>

<summary><strong>💡 Tips: なぜ Reaction が Context を更新するのか？</strong></summary>

この例では、Context を更新しているのは、`OperateDoor` (Interaction) ではなく、`ConsoleDoorReaction` です。

ここで多くの人は、「Interaction が Context を更新する方が自然な分離ではないか？」という疑問を持つでしょう。
結論から言うと、これは「ユーザー体験のクリーンさ」を保つための設計です。

まず、「反応（Reaction）とは何か？」という問題について考えます。
この問題について現実を参考に考えると、

**「反応」とは、「操作に対する状態の変化（あるいは変化しないこと）が、観察可能な形で現れる現象」** です。

アーキテクチャの話に戻ると、「反応」は「Reaction」、「操作」は「Operation」であり、「状態の変化」は「Context の更新」です。ここから、

**「Reaction は、Operation に対する Context の更新を観察可能な形で実行する」** という設計が導かれます。

アーキテクチャの性質としても、このように定義された Reaction はユーザー体験のクリーンさを保つことができます。
なぜなら、「Context の更新との対応が曖昧な Reaction」や、「Reaction を伴わない Context の更新」は、
ユーザーとシステムの関係をブラックボックス化し、ユーザー体験のクリーンさを低下させるからです。

このような関係のブラックボックスは、次のような例に代表されます。
- ボタンを押したのに何も起きていないように見える（内部状態だけ変わっている）
- 反応はあったが実際には反映されていない（内部状態が変わっていない）
- 待機中なのか、失敗しているのか分からない（内部状態との対応が曖昧）

この設計はユーザー体験だけでなく、開発者体験にもメリットがあります。
Reaction が Context を更新することで、Reaction の未実装や適用漏れが Context の未更新やUI上の無反応として自然に現れ、責務の漏れを早い段階で発見できます。

</details>

## 次のステップ (Next Step)

実践的な学習としての次のステップは、次の順序がおすすめです。
1. [Context Loop & System Flow](#context-loop-system-flow) をまだ読んでいない場合は、これを読みアーキテクチャとしての視点を学ぶ。
2. そのまま README を読み進め、[サンプル解説](#examples) を参考にサンプルでの実装例を学ぶ。
3. 「Hello Door」やその他のサンプルに、機能等を追加してみる。
  - 鍵付きのドアや、オンラインストレージを使ったNotepadなど
4. そのまま README を読み進め、[ライブラリの実装](#implementation) を参考に背景であるライブラリの実装を学ぶ。
5. 興味があれば、[Philosophy](#philosophy) を読み、背景となっている哲学や思想に触れ、理解を深める。

---

# サンプル解説 <a id="examples"></a>

各サンプルは、このリポジトリに実際に存在する `InteractionFlow.Samples.*` プロジェクトに対応しています。

## InteractionFlow.Samples.HelloDoor

`HelloDoor` は、最小構成の Context Loop を確認するためのサンプルです。

- 目的: Context によって同じ Interaction の結果が変わることを確認する
- Context: ドアが開いているか、閉まっているか、終了要求があるか
- Operation: `Open` / `Close` のキーワード入力
- Interaction: `OperateDoor`
- SystemFlow: `DoorSystemFlow`
- 見どころ: 独自 Port / Console External / Interaction / SystemFlow / Program の最小分割

最初に読むサンプルとして位置づけています。詳細な実装手順は [Hello Door 🚪](#hello-door-) にあります。

## InteractionFlow.Samples.Parrot

`Parrot` は、Console 標準実装と複数 SystemFlow の組み立てを確認するためのサンプルです。

- 目的: サンプル選択から実行までの会話型フローを確認する
- Context: 選択状態、キャンセル状態
- Storage: `ILastSelectMemory` による前回選択の保持
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
- Context: `NotepadContext`
- Storage: `ICurrentUserStoragePort` による現在ユーザーのセキュア情報、ユーザーデータ永続化、暗号化されたノートデータ
- Interaction: `EnterPassword`、`LoginSecure`
- 見どころ: Port / External の境界によって、既存 Flow を拡張できること

Port や Flow の差し替え可能な設計を確認できます。

---

# Philosophy <a id="philosophy"></a>

<p align="center">
    <img
        src="./docs/icon/icon_rich.svg"
        alt="Interaction Flow Architecture icon"
        width="128"
    >
</p>

Interaction Flow Architecture は、現実の相互作用を観察することから生まれました。

ソフトウェアを機能の集合ではなく、相互作用の流れとして捉え、その流れをコードによって直接設計することを目指しています。

その結果として、コードの構造とユーザー体験の構造が自然に対応し、開発品質とサービス品質を同じ視点で設計できます。

より詳しい思想や背景については、 [Interaction Flow - Philosophy](./docs/PHILOSOPHY.md) を参照してください。

---

# ライブラリの実装 <a id="implementation"></a>

このライブラリでは、Context / System Flow / Interaction / Function / System Flow Builder などのアーキテクチャ概念を型によって実装し、Analyzer によって制約を強化しています。

![Interaction Flow Architecture dependency diagram](./docs/img/InteractionFlowArchitecture_DependencyDiagram.svg)

代替テキスト: [Interaction Flow Architecture - Dependency Diagram Context](./docs/img/InteractionFlowArchitecture_DependencyDiagram.context.md)

ライブラリ内部の構造、パッケージ間の責務、Builder や Analyzer の位置づけについては、以下の資料を参考にしてください。
- ⭐ **[ライブラリの実装](./docs/LIBRARY_IMPLEMENTATION.md)**
- [.Core/.Standard/.Samples それぞれの役割](./docs/RoleOfMainProjects.md)
- [SystemFlow Builder の詳細](./docs/SystemFlowBuilder.md)
- [InteractionFlow.Analyzers](./InteractionFlow.Analyzers/README.md)

---

# 計算モデルとしての補助線 <a id="computational-model"></a>

計算モデルとしての Interaction Flow Architecture は、チューリングマシンのようなものとして説明できます。

この見方では、Function Port / Function External の子要素である Operation / Reaction / Storage / Silent External が、「読み取り、書き込み、外部への作用」を担うテープ操作に相当します。Interaction と Context の循環を、計算モデルとして理解しやすくするための見方です。

詳しい説明は [計算モデルとしての Interaction Flow アーキテクチャ](./docs/COMPUTATIONAL_MODEL.md) を参照してください。

---

# ロードマップ <a id="roadmap"></a>

Interaction Flow Architecture は、次の方向で発展させる予定です。

- Core API の安定化
- Standard API の拡充
- Analyzer による依存関係ルール検査の強化
- Unity Engine 向けの Standard.Unity API の開発
- その他ライブラリとしての改善候補の詳細は [ライブラリの実装 - 現在の制約と改善候補](./docs/LIBRARY_IMPLEMENTATION.md#future-improvements) を参照

---

# 資料まとめ <a id="references"></a>

- [.Core/.Standard/.Samples それぞれの役割](./docs/RoleOfMainProjects.md)
- [SystemFlow Builder の詳細](./docs/SystemFlowBuilder.md)
- [計算モデルとしての Interaction Flow アーキテクチャ](./docs/COMPUTATIONAL_MODEL.md)
- [Interaction Flow - Philosophy](./docs/PHILOSOPHY.md)
- [ライブラリの実装](./docs/LIBRARY_IMPLEMENTATION.md)
- [InteractionFlow.Analyzers](./InteractionFlow.Analyzers/README.md)

---

## 目次

[パッケージとインストール](#packages) | [ビジョン](#vision) | [Context Loop & System Flow](#context-loop-system-flow) | [はじめに](#getting-started) | [サンプル解説](#examples) | [Philosophy](#philosophy) | [ライブラリの実装](#implementation) | [計算モデルとしての補助線](#computational-model) | [ロードマップ](#roadmap) | [資料まとめ](#references)
