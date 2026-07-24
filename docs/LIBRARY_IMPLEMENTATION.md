[Interaction Flow Architecture](../README.md)

# ライブラリの実装

## 目次

- [全体像](#overview)
- [Context Loop の実行経路](#execution-path)
- [SystemFlow・Interaction・実行環境](#runtime-components)
- [`IFlowContext` インスタンスと終了結果](#context-and-results)
- [データ保持と永続化](#data-and-persistence)
- [設計上の保証とプロジェクト境界](#architecture-boundaries)
- [現在の制約と改善候補](#future-improvements)

## 全体像 <a id="overview"></a>

この章では、ドキュメントの対象範囲と、アーキテクチャ概念に対応する主な型を整理します。

### このページの目的 <a id="purpose"></a>

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

### README の概念とライブラリの対応 <a id="concept-mapping"></a>

README で定義している概念と、主に対応するライブラリ要素は次のとおりです。
これは概念全体と型の一対一対応ではなく、実装の中心となる型や実行経路です。

| アーキテクチャ概念 | 主なライブラリ要素 | 実装上の役割 |
| --- | --- | --- |
| `User` | 特定の型には固定されない | Operation を行い、Reaction を観測する主体 |
| `Context` | `IFlowContext` | System が次の相互作用に必要な文脈値を取得し、キャンセルを制御するためのインターフェース |
| `Context Loop` | `SystemFlow` が実行状態を参照しながら Interaction を選択・実行する処理 | Interaction の結果を次の選択と相互作用へつなげる |
| `System Flow` | `ISystemFlow<TContext>` / `SystemFlow<TContext>` | Interaction の合成、選択、継続、終了を構成する |
| `Interaction` | `IInteraction` / `Interaction` | Function Port と Domain の規則を組み合わせ、システム内部の目的を一段進める |
| `Operation` | `IOperationPort` とその実装 | User による操作や入力を受け取る |
| `Reaction` | `IReactionPort` とその実装 | `Context` の変化や処理結果を User が観測できる反応として表す |
| `Storage` | `IStoragePort` とその実装 | `Context` の文脈的な意味から独立して、再利用する値をメモリ上に保持する |
| `Silent External` | `ISilentExternalPort` とその実装 | User との相互作用やデータの記録を目的とせず、外部環境と連携する |
| `Function Port` | `IOperationPort`、`IReactionPort` など | Interaction から見える外部機能の契約 |
| `Function External` | Port を実装する Operation、Reaction、Storage など | Port の意味を Console、ファイル、OS などの実行環境へ接続する |
| `System Flow Builder` | `ScopeBuilder`、`SystemFlowBuilder<TContext>`、Handler | SystemFlow と依存オブジェクトを実行可能なスコープとして組み立てる |
| `Domain` | 主に `Entities` 名前空間の型 | 外部環境に依存しないデータ構造、規則、前提を表す |

`User` や `Context Loop` は、一つのクラスへ直接対応せず、複数の型による実行経路全体で表現されます。
`IFlowContext` が提供するのは System が扱う `Context` の一部であり、
User の認識や UI、Operation の戻り値、Reaction の出力までを集約するものではありません。
計算モデルでは、分散した文脈全体を `ContextTape` として整理していますが、`IFlowContext` と同じ実体ではありません。
詳しくは
[User と System の間にある ContextTape](./COMPUTATIONAL_MODEL.md#user-と-system-の間にある-contexttape)
を参照してください。

以降では、アーキテクチャ上の概念を `Context`、
実行時に API 間で受け渡されるオブジェクトを `IFlowContext` インスタンスと表記します。
Function の分類は物理的な配置ではなく、利用目的に基づきます。
User から System への相互作用を構成するものは Operation、データの記録を目的とするものは Storage、
そのどちらにも該当しない外部環境との連携は Silent External です。

README とアーキテクチャ図の `Storage` は、メモリ上の一時データから
DB やファイルシステムの永続データまでを含む広い概念です。
ライブラリでは、この概念を、メモリ上の値を所有する `IStoragePort` / `Storage`、
外部保存先と読み書きする `IPersistencePort`、保存形式を変換する `ISerializerPort`
へ分割して実装しています。

## Context Loop の実行経路 <a id="execution-path"></a>

この章では、Context Loop の実行経路を、汎用的な構造と Hello Door の具体例から確認します。

### Context Loop がライブラリ上で実行されるまで <a id="context-loop-execution"></a>

Context Loop は専用の `ContextLoop` クラスではありません。
ライブラリでは、そのうち System 側の処理を、SystemFlow が実行状態に応じて
Interaction を選択・実行し、結果を次の相互作用へつなぐ経路として表現します。
同じ `IFlowContext` インスタンスの継続利用は代表的な構成ですが、API 上の必須条件ではありません。

```text
Program
  ├─ Builder で実行環境を構築する
  ├─ IFlowContext インスタンスを準備する
  └─ SystemFlowHandler
       └─ SystemFlow
            ├─ IFlowContext、直前の終了結果、依存オブジェクトを参照する
            └─ Interaction を選択・実行する
                 ├─ Operation / Storage / Silent External
                 ├─ Domain の規則
                 └─ Reaction
                      └─ User が観測可能な反応と、必要に応じた文脈値の更新
```

一回の Interaction が完了しても、Context Loop 全体が終了するとは限りません。
SystemFlow は、現在の `IFlowContext` インスタンス、Interaction の終了結果、
許可された依存を通じて観測できる Domain の状態などから、次の Interaction を実行するか、
`FlowEndToken` を返して終了するかを決めます。

### Hello Door で見る一周の流れ <a id="hello-door-flow"></a>

`InteractionFlow.Samples.HelloDoor` では、この実行経路を次の型が担当します。

```text
Program
  └─ DoorSystemFlow
       └─ OperateDoor
            ├─ IDoorOperation
            └─ IDoorReaction
                 └─ DoorState を更新し、結果を表示する
```

| 段階 | 担当 | 主なソース |
| --- | --- | --- |
| 実行環境の選択 | `Program` が Port 実装を Builder へ登録する | [`Program.cs`](../InteractionFlow.Samples.HelloDoor/Program.cs)、[`SystemFlowBuilder.cs`](../InteractionFlow.Standard/Builders/SystemFlowBuilder.cs) |
| `IFlowContext` インスタンスの準備 | `FlowContext` に `DoorState` を重ねる | [`FlowContext.cs`](../InteractionFlow.Core/Entities/Contexts/FlowContext.cs)、[`ScopedFlowContext.cs`](../InteractionFlow.Core/Entities/Contexts/ScopedFlowContext.cs)、[`DoorState.cs`](../InteractionFlow.Samples.HelloDoor/Entities/DoorState.cs) |
| 継続の判断 | `DoorSystemFlow` が終了要求まで `OperateDoor` を繰り返す | [`DoorSystemFlow.cs`](../InteractionFlow.Samples.HelloDoor/SystemFlows/DoorSystemFlow.cs)、[`SystemFlow.cs`](../InteractionFlow.Core/SystemFlows/SystemFlow.cs) |
| 相互作用 | `OperateDoor` が Operation の結果である `DoorCommand` を Reaction へ渡す | [`OperateDoor.cs`](../InteractionFlow.Samples.HelloDoor/Interactions/OperateDoor.cs)、[`Interaction.cs`](../InteractionFlow.Core/Interactions/Interaction.cs)、[`DoorCommand.cs`](../InteractionFlow.Samples.HelloDoor/Entities/DoorCommand.cs) |
| 相互作用 - Operation | `IDoorOperation` が `DoorCommand` を結果として返す | [`IDoorOperation.cs`](../InteractionFlow.Samples.HelloDoor/ExternalPorts/OperationPorts/IDoorOperation.cs)、[`ConsoleDoorOperation.cs`](../InteractionFlow.Samples.HelloDoor/Externals/Operations/ConsoleDoorOperation.cs) |
| 相互作用 - Reaction | `IDoorReaction` が `DoorState` を更新して結果を表示する | [`IDoorReaction.cs`](../InteractionFlow.Samples.HelloDoor/ExternalPorts/ReactionPorts/IDoorReaction.cs)、[`ConsoleDoorReaction.cs`](../InteractionFlow.Samples.HelloDoor/Externals/Reactions/ConsoleDoorReaction.cs) |

実装手順とコード全体は、
[Interaction Flow Architecture - Hello Door 🚪](../README.md#hello-door-) を参照してください。
Reaction と `Context` 更新の対応は設計原則であり、型による保証範囲については
[ライブラリが保証することと設計原則](#guarantees-and-principles) にまとめています。

## SystemFlow・Interaction・実行環境 <a id="runtime-components"></a>

この章では、フローを構成する実行単位と、それらを実行可能な環境へ組み立てる仕組みを説明します。

### SystemFlow の実装 <a id="systemflow"></a>

`ISystemFlow<TContext>` は、指定した `IFlowContext` 実装型で SystemFlow を実行する契約です。

```csharp
Task<FlowEndToken> ExecuteAsync(TContext context);
```

`SystemFlow<TContext>` 基底クラスは、派生クラスの `ExecuteCoreAsync` を実行し、
返された `FlowEndToken` の終了結果を、SystemFlow に渡された `IFlowContext` インスタンスへ結び直します。

派生 SystemFlow が主に決めるのは次の内容です。

- どの Interaction、または別の SystemFlow を合成するか
- どの順序、条件分岐、反復によって実行するか
- `IFlowContext` インスタンス、終了結果、許可された依存のどの情報を選択や継続条件に使うか
- どの時点で SystemFlow を終了するか

SystemFlow 基底クラスがループや分岐方法を固定しているわけではありません。
逐次実行、条件分岐、繰り返し、別の SystemFlow の実行などを、
派生クラスがユーザー体験に合わせて構成します。

### Interaction の実装 <a id="interaction"></a>

`Interaction` は、Function Port による入出力やデータ操作と Domain の規則を組み合わせ、
相互作用に関するシステム内部の目的を一段進める基底クラスです。
複数の Function 呼び出しや Domain の計算を含めても、SystemFlow からは一つの実行単位として扱われます。

派生クラスが実装する中心処理は次のメソッドです。

```csharp
protected abstract Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context);
```

基底クラスは、その周囲で次の共通処理を行います。

- 実行前のキャンセル確認
- キャンセル対象タスクの登録
- `OperationCanceledException` の Cancellation Port への委譲
- その他の例外の Exception Port への委譲
- `ReactionEnd` と実行時の `IFlowContext` インスタンスから `FlowEndToken` を作成

これにより、派生 Interaction は例外表示やキャンセル表示の実装を直接持たず、
Interaction 固有の Function の組み合わせへ集中できます。

Interaction は、Operation、Storage、Silent External のすべてを呼ぶ必要はなく、
User からの入力を必要としない Interaction では Operation を含まない構成も可能です。
一方、正常完了時の `ReactionEnd` は Reaction 系 API から取得するように制限されています。
ただし、基底クラスは User に観測可能な Reaction が実際に行われたことや、
その反応と `Context` の更新が対応していることまでは検証しません。
これらは Port の設計、実装、Analyzer、コードレビューによって維持します。

### Function Port と Function External <a id="function"></a>

#### Function Port

Function Port は、Interaction から見える外部機能の契約です。

ライブラリでは、Function を次の種類へ分類します。

| 種類 | Port | 意味 |
| --- | --- | --- |
| Operation | `IOperationPort` | User による操作や入力を受け取る |
| Reaction | `IReactionPort` | User が観測できる反応を提供する |
| Storage | `IStoragePort` | `Context` の文脈的な意味から独立して値を保持する |
| Silent External | `ISilentExternalPort` | User との相互作用や記録を目的とせず、外部環境と連携する |

Port は「Console へ表示する」「特定の DB へ保存する」といった実現方法ではなく、
Interaction が必要とする機能の意味を定義します。

#### Function External

Function External は、Port の契約を実行環境へ接続する実装です。

```text
Interaction
    ↓ 依存
Function Port
    ↑ 実装
Function External
    ↓ 利用
Console / File System / OS / External Service
```

ソース配置と Analyzer では、通常次の namespace により Port と実装を分けます。

- `ExternalPorts`: Port 契約
- `Externals`: Port の実装

#### 現在のレイヤーメタデータに関する注意

`FlowLayerTypes` には `FunctionPort` と `FunctionExternal` の両方があります。
一方、現在の `Operation`、`Reaction`、`Storage`、`SilentExternal` 基底実装は、
`IFlowNode.Layer` として `FunctionPort` を返します。

そのため、現時点で Port と External の区別を実際に表しているのは、主に次の二つです。

- `ExternalPorts` / `Externals` というソースと namespace の境界
- Analyzer のレイヤー依存規則

`IFlowNode.Layer` の実行時メタデータだけでは、両者を区別できません。
これはアーキテクチャ図と実装メタデータの対応を今後整理する必要がある点です。

#### Function が保持する状態

Operation、Reaction、Storage、Silent External の Port は `IFlowNodeStateful` を継承し、
実装が保持するメモリ上の状態を明示的に初期化する `ForceResetMemoryState` を定義します。

`Context` が Interaction 間で引き継ぐ文脈であるのに対し、Function の状態は
外部機能の設定やキャッシュ、入力の加工履歴、出力効果の進行状態など、
Function が自身の振る舞いを実現するために保持する内部状態です。
これらは `IFlowContext` が提供する文脈値とは別に管理されますが、
Function の実行結果を通じて後続の Interaction の選択に影響することがあります。
`ForceResetMemoryState` はスコープ破棄時に自動実行されないため、
強制的な再初期化が必要な場合に呼び出し側が使用します。

`IHasFunctionState<TState>` を実装する Function は、
`FunctionStateScope<TState>` により状態を一時的に差し替え、破棄時に元へ戻せます。

### Builder と実行スコープ <a id="builder"></a>

Builder と Handler は、依存オブジェクトの構築と実行スコープの寿命を分担します。

| 型 | 役割 |
| --- | --- |
| `ScopeBuilder` | 登録情報から依存解決スコープを一つ構築する |
| `ScopeHandler` | 構築済みスコープを保持し、親スコープも含めて依存を解決する |
| `SystemFlowBuilder<TContext>` | SystemFlow と専用スコープを構築する |
| `SystemFlowHandler<TContext>` | SystemFlow の実行と専用スコープの寿命を管理する |

Builder は一度 Build すると再利用できません。標準登録はスコープ単位で共有され、
子スコープで解決できない依存は親から解決されます。Handler の破棄は自身のスコープを
無効にしますが、親スコープや外部から渡された `IFlowContext` インスタンスは破棄しません。

Builder の詳細は、[SystemFlow Builder の詳細](./SystemFlowBuilder.md) も参照してください。

### 実行時の依存ノードツリー <a id="dependency-tree"></a>

SystemFlow、Interaction、Function Port 実装は `IDependencyNode` として、
実行時に解決された具体的な依存インスタンスを `Dependency` に保持します。

```text
DoorSystemFlow
  └─ OperateDoor
       ├─ ConsoleExceptionHandling
       ├─ ConsoleCancellationHandling
       ├─ ConsoleDoorOperation
       └─ ConsoleDoorReaction
```

`SystemFlowHandler.Root` は、生成された SystemFlow の実行時インスタンスを
`IDependencyNode` として公開します。`DependencyTreeView.GetDependencyTreeText` は、
この Root から実体を再帰的に表示します。これは DI の登録一覧ではなく、
実行するフローを根とした観察用の構造です。

依存宣言の検査は [Analyzer による設計支援](#analyzer) にまとめています。

## `IFlowContext` インスタンスと終了結果 <a id="context-and-results"></a>

この章では、フロー間で引き継がれる `IFlowContext` インスタンスと、
Reaction から SystemFlow まで伝播する終了結果を説明します。

### `IFlowContext` の実装 <a id="context"></a>

#### IFlowContext が提供する最小契約

`IFlowContext` は、`Context` の具体的なデータ構造を固定しません。

```csharp
public interface IFlowContext
{
    CancellationObject Cancellation { get; }

    bool TryGet<T>(out T value);
}
```

提供するのは次の二つです。

- その `IFlowContext` インスタンスに紐づくキャンセル制御
- 型を指定して文脈値を取得する仕組み

Interaction は、特定の `IFlowContext` 実装へキャストする代わりに、
原則として `TryGet<T>` により必要な文脈値を要求できます。

`TryGet<T>` は、要求型の値が存在しない場合に `false` を返します。
ただし、あらゆる失敗を `false` に変換する API ではありません。
`ScopedFlowContext` では、Entry の循環参照など、単なる値の不在や型不一致ではない
解決失敗を検出した場合は例外を送出します。
破棄済みの `ScopedFlowContext` に対する呼び出しも `ObjectDisposedException` になります。

#### `IFlowContext` インスタンスが提供する文脈値の読み取りと更新

`IFlowContext` の基本契約は `TryGet<T>` による読み取りです。
任意の値を置換する `Set<T>` や、値を削除する `Remove<T>` は定義していません。

その意味で、`IFlowContext` インスタンスが公開する値へのアクセスは基本的に ReadOnly です。
ただし、これは取得した参照型オブジェクトの内部状態まで不変にするという意味ではありません。
Hello Door の `DoorState` のような mutable な参照型を取得した場合、そのプロパティは更新できます。

値そのものを置き換える必要がある場合は、`RefEntry<TValue>` を
`ScopedFlowContext` インスタンスに追加できます。
`Entry<TValue>.Value` は外部からは読み取り専用ですが、
`RefEntry<TValue>` は `Value` の setter を公開します。

```csharp
using var context = new ScopedFlowContext(new FlowContext())
    .With(new RefEntry<int>(0));

if (context.TryGet<RefEntry<int>>(out var count))
{
    count.Value++;
}
```

同じ `IFlowContext` インスタンスから `TryGet<int>` を呼ぶと、
`RefEntry<int>` の内側にある現在値を取得できます。
ラッパー自体が必要な場合は `TryGet<RefEntry<int>>` を使用し、その `Value` を更新します。

#### FlowContext

`FlowContext` は `CancellationObject` を持つ最小実装です。
基本実装の `TryGet<T>` では、この `CancellationObject` 自身を文脈値として取得できます。

`FlowContext` は現在の `Context` を扱う最小の `IFlowContext` 実装であり、
それ単独で Context Loop 全体を表すものではありません。
`IFlowContext` インスタンスが Interaction 間で引き継がれ、
それが提供する文脈値が更新され続ける実行過程全体によって、
Context Loop が形づくられます。

#### ScopedFlowContext

`ScopedFlowContext` は、親の `IFlowContext` インスタンスが提供する値を参照しながら、
一時的な値を追加します。

```text
ScopedFlowContext
  ├─ 新しく追加された値を新しい順に探索する
  └─ 見つからなければ親 IFlowContext インスタンスを探索する
```

同じ型の値を複数追加した場合は、後から追加された値が先に見つかります。
追加値が `Entry` の場合は、Entry が保持する値も再帰的に探索されます。

`ScopedFlowContext.Dispose` は、追加値を破棄しません。
内部の探索リストを解放し、追加値の操作と探索を利用不能にします。
破棄後に `With` または `TryGet<T>` を呼ぶと `ObjectDisposedException` が発生します。
`Cancellation` は探索リストを使用せず親の `IFlowContext` インスタンスへ委譲するため、
破棄後も取得できます。

追加値が `IDisposable` であり、破棄が必要な場合、その値の所有者が別途破棄する必要があります。

#### CancellationObject のライフサイクル

`CancellationObject` は、キャンセル要求に使用するトークンと、
キャンセル時に完了を待つタスクを `IFlowContext` インスタンス単位で管理します。

```text
処理が GetToken() で CancellationToken を取得する
    ↓
Interaction がキャンセル対象タスクを登録する
    ↓
Cancel() がトークンへキャンセルを通知する
    ↓
処理が OperationCanceledException で終了する
    ↓
CancellationHandling 基底実装が WaitAndResetAsync() を実行する
    ├─ 登録済みタスクの完了を待つ
    └─ キャンセル状態をリセットする
    ↓
同じ IFlowContext インスタンスを次の処理へ再利用できる
```

`Cancel()` はタスクを強制終了しません。
キャンセル対象の処理が `GetToken()` で取得したトークンを監視し、
キャンセル要求へ応答する必要があります。

標準の `CancellationHandling` がキャンセル処理後に状態をリセットするのは、
一度キャンセルされた `IFlowContext` インスタンスを永久にキャンセル済みのままにせず、
Context Loop の次の処理へ再利用できるようにするためです。

#### ネストした Interaction の `IFlowContext` インスタンス

`Interaction.NestedExecuteAsync` は、親の `IFlowContext` インスタンスが提供する文脈値を
参照できる内部の `IFlowContext` インスタンスを作成します。
この内部インスタンスは親とは別の `CancellationObject` を持ち、親のキャンセル要求と連動します。

独立したキャンセル制御を使用する主な理由は、キャンセル待機処理の循環待機を避けるためです。
親 Interaction と子 Interaction が同じ `CancellationObject` を使用すると、
両方のキャンセル対象タスクが同じ待機一覧へ登録されます。
その状態で子のキャンセル処理が一覧全体の完了を待つと、次の循環が発生する可能性があります。

```text
親 Interaction
  └─ 子 Interaction の完了を待つ
       └─ CancellationObject が親 Interaction の完了を待つ
            └─ 親 Interaction は子 Interaction を待っている
```

ネストした Interaction に専用の `CancellationObject` を与えることで、
子のキャンセル待機処理は子スコープ内のタスクだけを待機します。
同時に、親のキャンセルトークンを子の `Cancel` へ登録することで、
親から子へのキャンセル伝播は維持します。

したがって、すべての `IFlowContext` インスタンスが
常に同じ `CancellationObject` を共有するわけではありません。

- `ScopedFlowContext` インスタンスは親の `CancellationObject` を共有する
- ネスト実行用の `IFlowContext` インスタンスは独立したキャンセル制御を持ち、親キャンセルと連動する

という違いがあります。

ネストした Interaction が未解決の例外またはキャンセルを含む `ReactionEnd` を返した場合、
`NestedExecuteAsync` はその例外を親 Interaction へ再送出します。
これにより、親 Interaction の `ExecuteAsync` が、親側の Exception Port または
Cancellation Port を通じて結果をもう一度処理できます。

#### `IFlowContext` インスタンスの所有権

`SystemFlow.ExecuteAsync` と `Interaction.ExecuteAsync` は、
渡された `IFlowContext` インスタンスを破棄しません。

`IFlowContext` インスタンスの寿命は、次のように考えます。

```text
IFlowContext インスタンスを作成した呼び出し側
  ├─ SystemFlow へ渡す
  ├─ 必要なら次の SystemFlow へ再利用する
  └─ 不要になった時点で破棄する
```

### ReactionEnd と FlowEndToken <a id="reaction-end-flow-end-token"></a>

#### ReactionEnd

`ReactionEnd` は、Reaction 系 API が確定した成功、未解決例外、
キャンセル例外を表します。コンストラクターは `internal` であり、
通常は Reaction Port または Reaction 基底クラスの `GetEnd` から生成します。
これは終了結果を Reaction 系の契約へ寄せる制約ですが、
User が実際に反応を観測したことまでは証明しません。

#### FlowEndToken

`FlowEndToken` は `ReactionEnd` と、
そのフローの実行に渡された `IFlowContext` インスタンスを結びつけます。

```text
FlowEndToken
  ├─ LastContext: そのフローの ExecuteAsync に渡された IFlowContext インスタンス
  └─ End: ReactionEnd
```

Interaction は Reaction の結果を実行時の `IFlowContext` インスタンスと結合し、
SystemFlow はその `End` を SystemFlow 自身に渡された `IFlowContext` インスタンスへ結び直します。
このため `LastContext` は内部の `IFlowContext` インスタンスの全来歴ではなく、
現在の実行境界を表します。`FlowEndToken` は `IFlowContext` インスタンスを破棄せず、
所有権も取得しません。

#### Result、ReactionEnd、FlowEndToken の役割

ライブラリでは、処理の範囲に応じて三種類の結果表現を使い分けます。

| 結果型 | 主な範囲 | 表すもの |
| --- | --- | --- |
| `Result` / `Result<TValue>` | Function や補助処理 | 局所処理の成功、成功値、または失敗 |
| `ReactionEnd` | Reaction / Interaction | Reaction 系 API が確定した Interaction の終了結果 |
| `FlowEndToken` | Interaction / SystemFlow 境界 | 終了結果と、その実行境界へ渡された `IFlowContext` インスタンス |

`Result` の失敗が、自動的に `ReactionEnd` へ変換されるわけではありません。
Interaction や Reaction は、Function が返した失敗をどのように処理し、
User が観測できる反応や未解決例外へ変換するかを決めます。

- `default(Result)` は成功として扱われる
- `default(Result<TValue>)` は成功値を持たないため失敗として扱われる
- `Result<TValue>` の成功値に `null` は使用できない
- `Exception` 派生型の値は成功値ではなく、失敗として扱われる
- `Then` は成功時だけ次の処理へ進み、失敗はそのまま伝播する
- `ThenError` は失敗時だけ回復処理を実行する

`Result` は例外を自動捕捉しません。失敗として伝播させる場合は、
処理側が例外を失敗 `Result` へ変換します。

<a id="supporting-mechanisms"></a>

## データ保持と永続化 <a id="data-and-persistence"></a>

ここまでの型が Context Loop の主要な実行経路です。
データの保持と永続化は、次の機構が分担します。

| 要素 | 責務 |
| --- | --- |
| Entry | 値へ型解決や更新可能性を与える |
| Storage | メモリ上の値を作成・保持・破棄する |
| PersistentEntry | 値と永続化先 ID を関連付ける |
| Persistence | 保存先との読み書きを行う |
| Serializer | 値と保存形式を変換する |

### Entry

`Entry<TValue>` は、値をラップし、要求された型として値を解決するための基底クラスです。
別の Entry を保持する場合は再帰的に解決し、循環参照は失敗結果になります。
`RefEntry<TValue>` は setter を公開し、`IFlowContext` インスタンスや Storage に置いた値を
明示的に差し替えられるようにします。

### Storage

`IStoragePort` は、メモリ上に生成した値をキー単位で保持する Function Port です。

```text
IFlowContext インスタンス
  └─ Storage のキーを提供する
        ↓
Storage
  ├─ 既存値を取得する
  ├─ 値がなければ作成する
  └─ 値を削除し、必要なら破棄する
```

`Storage<TKey, TValue>` は、Dictionary を使う外部副作用に依存しない既定基底実装です。
`GetOrCreate` で作成して登録した値は、Storage が所有するメモリ上の値として扱われます。

#### IFlowContext インスタンスから Storage のキーを取得する

`GetKey(IFlowContext)` は、現在の `IFlowContext` インスタンスから
Storage のキーを取得する契約です。
既定実装は `context.TryGet<TKey>` を使用し、取得できなければ失敗 `Result<TKey>` を返します。
派生 Storage はこの処理を差し替え、複数の文脈値からキーを構成できます。

Storage は汎用 `Set` API を公開せず、登録関係を作成・削除操作で管理します。
保持値の差し替えが必要な場合は、`TValue` に `RefEntry<T>` を使用できます。

#### 作成・削除可能性を Result で制御する

派生 Storage は `CreateNewValue` と `CanRemoveValue` の `Result` により、
作成できるキーと削除できる登録を制御します。次の例は、正のキーだけを作成し、
キー `1` の削除を禁止します。

```csharp
private sealed class MessageStorage : Storage<int, string>
{
    protected override Result<string> CreateNewValue(int key) =>
        key > 0
            ? $"Message {key}"
            : new ArgumentOutOfRangeException(nameof(key));

    protected override Result CanRemoveValue(int key, string value) =>
        key == 1
            ? new InvalidOperationException("Key 1 cannot be removed.")
            : Result.Success;
}
```

作成または削除判定が失敗した場合、Storage の登録状態は変更されません。
Clear 系も、すべての値を削除可能と確認してから処理します。
`Result` の例外処理を含む共通動作は、
[ReactionEnd と FlowEndToken](#reaction-end-flow-end-token) を参照してください。

#### 通常削除と強制リセット

| 操作 | 動作 |
| --- | --- |
| `RemoveAndDispose` | 削除判定後、登録を外して値を破棄する |
| `RemoveWithoutDispose` | 削除判定後、値を破棄せず登録だけを外す |
| `ClearAndDispose` | 全値の削除判定後、値を破棄して全件クリアする |
| `ClearWithoutDispose` | 全値の削除判定後、値を破棄せずに全件クリアする |
| `ForceResetMemoryState` | 削除判定を行わず、破棄例外を集約して登録状態をクリアする |

通常操作は `CanRemoveValue` を尊重します。`ForceResetMemoryState` は通常規則より
再初期化を優先する強制操作です。破棄処理が例外を送出する場合は、
通常操作ではその例外が呼び出し側へ伝播します。

### PersistentEntry

`PersistentEntry<TPersistenceId, TValue>` は、メモリ上の値と永続化先 ID を関連付けます。

| 操作 | メモリ上の値への影響 |
| --- | --- |
| `Load` | 成功時だけ読み込んだ値へ置き換える |
| `DeleteAndReset` | 削除成功時だけ `default` へ戻す |
| `Save` | 値を変更しない。`null` は保存せず失敗を返す |
| `Exists` | 値を変更しない |

`Load` は既存値を再利用先として渡せますが、実装がその内部状態を変更してから失敗した場合、
変更までは自動的にロールバックしません。また、置き換え前の値は自動破棄されないため、
必要な場合は呼び出し側または派生 Entry が所有方針を決めます。

### Persistence

`IPersistencePort<TPersistenceId, TValue>` は `Save`、`Load`、`Delete`、`Exists`、
`GetAllIds` を定義し、保存先との読み書きを担当します。メモリ上の値の所有は Storage、
保存形式への変換は Serializer の責務です。

値と既存値は `Result` のまま渡されます。実装は上流の失敗を伝播または回復でき、
`Load` では既存オブジェクトを再利用することもできます。

Persistence と Serializer は `IFlowNode` として Function 種別を持つ Port ではなく、
Storage / Persistence 機構を構成する `IFlowSubNode` として依存ノードグラフへ参加します。

Standard の `FilePersistence<TFileId, TValue>` は、
ファイルシステムを保存先とする Persistence の基底実装です。

### Serializer

`ISerializerPort<TData, TValue>` は、値と保存・転送用データ形式の相互変換を定義します。

```text
TValue
  ↓ Serialize
TData
  ↓ Persistence
External Storage
```

Standard の `TextSerializer<TValue>` は、
値と文字列の変換を、UTF-8 の Stream 読み書きへ接続します。

入力と参照値は `Result` で渡されます。`inputValue` / `inputData` は変換対象と取得失敗、
`refData` / `refValue` は再利用可能な書き込み先や既存オブジェクトを表します。
再利用するか新しい値を生成するかは Serializer 実装が決め、入力取得から変換までの失敗を
同じ Result パイプラインで返します。

## 設計上の保証とプロジェクト境界 <a id="architecture-boundaries"></a>

この章では、ライブラリと Analyzer が保証する範囲、および各プロジェクトの責務境界を整理します。

### ライブラリが保証することと設計原則 <a id="guarantees-and-principles"></a>

Interaction Flow Architecture のすべての意味が、C# の型だけで完全に保証されるわけではありません。

| 内容 | 現在の実装 |
| --- | --- |
| SystemFlow が受け取る `IFlowContext` 実装型 | ジェネリック型制約と API が規定する |
| SystemFlow が必ず例外・キャンセル時に終了する | 派生 SystemFlow の継続条件に依存する |
| Interaction の正常完了時の戻り値が `FlowEndToken` である | `IInteraction` のメソッドシグネチャが規定する |
| Interaction 基底クラスが例外とキャンセルを Port へ委譲する | `Interaction` 基底実装が提供する |
| `ReactionEnd` の生成が Reaction 系の契約を経由する | `internal` コンストラクタと Reaction Port の `GetEnd` が制限する |
| Reaction が必ず User に観測される | 原理上保証できない |
| `IFlowContext` インスタンスの値を型で取得する | `IFlowContext.TryGet<T>` が提供する |
| `IFlowContext` が提供する文脈値の更新が Reaction 内だけで行われる | 現在は保証しない。設計上の理想として推奨し、実用性を検証している |
| Interaction の結果が `IFlowContext` インスタンスの文脈値へ適切に反映される | 設計とレビューに依存する |
| namespace 依存方向 | Analyzer が有効な場合に検査する |
| namespace 名を使わずに意味的なレイヤーを完全判定する | 現在の Analyzer では保証しない |

Exception Port が例外を再送出する設定の場合や、例外・キャンセル処理自体が例外を送出した場合、
`Interaction.ExecuteAsync` は `FlowEndToken` を返さず、例外が呼び出し側へ伝播します。

型による制約は、設計判断を不要にするためのものではありません。
README と Philosophy が示す「User と System の関係」をコード上でも追えるようにするための支援です。

Interaction Flow Architecture では、Reaction が User に観測可能な反応を提供し、
その反応と対応して、次の相互作用に必要な `Context` が更新されることを理想とします。
一方、`IFlowContext` から取得した可変オブジェクトは、現在の型ではどのレイヤーからも変更できます。
この更新を Reaction だけに制限しても実務上の問題が生じないことは、
まだ十分な利用例によって確認されていません。
そのため、現在の型と Analyzer は、`IFlowContext` が提供する文脈値の更新経路を制限していません。
今後、利用例と実装経験が蓄積し、実用上の問題がないと判断できた場合は、
この理想を検査可能な具体的制約へ昇格させる予定です。

### Analyzer による設計支援 <a id="analyzer"></a>

`InteractionFlow.Analyzers` は、アーキテクチャの境界をコード編集中に確認するための
Roslyn Analyzer です。

| 診断 | 検査内容 |
| --- | --- |
| `InteractionFlowArchitecture001` | namespace のレイヤー名から依存方向を検査する |
| `InteractionFlowArchitecture002` | `IDependencyNode` の依存引数が実行時グラフから欠落しないか検査する |

Analyzer は `interactionflow_enabled = True` の場合に有効になります。対象レイヤー名を含まない
namespace、`Context` 更新と Reaction の意味的な対応、複雑な依存グラフ全体までは検査しません。
このリポジトリでは `.editorconfig` で有効化し、診断モードを `Error` にしています。

詳細は [InteractionFlow.Analyzers](../InteractionFlow.Analyzers/README.md) を参照してください。

### パッケージ境界との対応 <a id="package-boundaries"></a>

| プロジェクト | 役割 |
| --- | --- |
| `InteractionFlow.Core` | `Context`、SystemFlow、Interaction、Port などの概念と基本契約を定義する |
| `InteractionFlow.Standard` | DI、Console、FileSystem、Serializer などの標準実装を提供する |
| `InteractionFlow.Samples.*` | Core と Standard を具体的な Context Loop として組み立て、API を検証する |

詳細は [.Core/.Standard/.Samples それぞれの役割](./RoleOfMainProjects.md) を参照してください。

## 現在の制約と改善候補 <a id="future-improvements"></a>

以下は互換性を約束する Roadmap ではなく、利用例とサンプルで妥当性を確認しながら
判断する設計上の検討事項です。現状これらの問題に対する判断の責務はライブラリ利用者にあります。現在の挙動は各本文を参照してください。

| 課題 | 現在の制約 | 検討方向 |
| --- | --- | --- |
| `IFlowContext` の文脈値更新 | 取得した参照型はどのレイヤーからも変更できる（[`IFlowContext` の実装](#context)） | 利用例を通じて Reaction に更新を限定しても問題がないか検証し、妥当性を確認できた場合は型または Analyzer の制約へ昇格する |
| Entry の所有権 | `IFlowContext` インスタンス、Storage、PersistentEntry で破棄責務が統一されていない（[データ保持と永続化](#data-and-persistence)） | 所有・借用と置換時の破棄を型または API で表す |
| ReactionEnd と Result | 終了結果と局所結果が別の型である（[ReactionEnd と FlowEndToken](#reaction-end-flow-end-token)） | Reaction 固有の生成制約を保ったまま内部表現を共通化する |
| Function のレイヤー情報 | External の実体も `FunctionPort` として見える場合がある（[Function Port と Function External](#function)） | 契約上の分類と実行時レイヤーを分離する |
| SystemFlow の終了結果 | 継続・中断の判断は派生 SystemFlow に委ねられる（[SystemFlow の実装](#systemflow)） | 合成 API や未確認結果の Analyzer 支援を検討する |
| 同一スコープの並行実行 | 共有される可変状態の並行安全性を保証しない。通常は逐次実行し、並行時は Scope と `IFlowContext` インスタンスを分ける | 原則を逐次実行とするか、状態分離・同期を導入する |
| ライフタイム | Scope、`IFlowContext` インスタンス、Cancellation、Entry、Function を統一する所有権モデルがない（[Builder と実行スコープ](#builder)） | 非同期破棄を含む共通規則を整理する |
| 依存グラフ | 循環を検査せず、再合流の表示方法も定義していない（[実行時の依存ノードツリー](#dependency-tree)） | DAG として検証し、再合流を参照として表示する |
| Analyzer の範囲 | namespace 外の型や意味的規則は検査しない（[Analyzer による設計支援](#analyzer)） | Layer 判定と、型だけでは保証できない規則の支援範囲を拡張する |

---

## 目次

[全体像](#overview) | [Context Loop の実行経路](#execution-path) | [SystemFlow・Interaction・実行環境](#runtime-components) | [`IFlowContext` インスタンスと終了結果](#context-and-results) | [データ保持と永続化](#data-and-persistence) | [設計上の保証とプロジェクト境界](#architecture-boundaries) | [現在の制約と改善候補](#future-improvements)

[Interaction Flow Architecture](../README.md)
