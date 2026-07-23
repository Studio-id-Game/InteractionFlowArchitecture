[README に戻る](./README.md)

# ライブラリの実装

## このページの目的

README では、Interaction Flow Architecture を次のような Context Loop として説明しています。

```text
Context → Interaction → next Context → next Interaction → ...
```

このページでは、この流れがライブラリ上でどのように実行されるかを説明します。

中心にするのは、個々の API の網羅的なリファレンスではありません。

- README のアーキテクチャ概念が、どの型に対応するか
- `Program` から `SystemFlow`、`Interaction`、`Function` へ、処理がどう進むか
- Context がどこで参照・更新され、次の Interaction へどう引き継がれるか
- ライブラリが型として保証することと、設計原則として期待することの違い
- Builder、Storage、Analyzer が Context Loop の実装をどう支えるか

を、実際のソースコードに沿って整理します。

個別 API の引数、戻り値、例外などの詳細は、各型の XML ドキュメントコメントも参照してください。

## README の概念とライブラリの対応

README で定義している概念と、主に対応するライブラリ要素は次のとおりです。

| アーキテクチャ概念 | 主なライブラリ要素 | 実装上の役割 |
| --- | --- | --- |
| `User` | 特定の型には固定されない | Operation を行い、Reaction を観測する主体 |
| `Context` | `IFlowContext` | 現在の相互作用に必要な文脈値とキャンセル制御を提供する |
| `Context Loop` | `SystemFlow` が Context を参照しながら Interaction を実行する処理 | Interaction の結果を次の Interaction へ引き継ぐ |
| `System Flow` | `ISystemFlow<TContext>` / `SystemFlow<TContext>` | Interaction の順序、継続、終了を構成する |
| `Interaction` | `IInteraction` / `Interaction` | Function Port を組み合わせ、システム内部の目的を一段進める |
| `Operation` | `IOperationPort` とその実装 | User の操作や外部条件を入力として受け取る |
| `Reaction` | `IReactionPort` とその実装 | Context の変化や処理結果を User が観測できる反応として表す |
| `Storage` | `IStoragePort` とその実装 | Context とは別の寿命で、再利用する値をメモリ上に保持する |
| `Silent External` | `ISilentExternalPort` とその実装 | User に直接観測されない外部環境とのやりとりを行う |
| `Function Port` | `IOperationPort`、`IReactionPort` など | Interaction から見える外部機能の契約 |
| `Function External` | Port を実装する Operation、Reaction、Storage など | Port の意味を Console、ファイル、OS などの実行環境へ接続する |
| `System Flow Builder` | `ScopeBuilder`、`SystemFlowBuilder<TContext>`、Handler | SystemFlow と依存オブジェクトを実行可能なスコープとして組み立てる |
| `Domain` | 主に `Entities` 名前空間の型 | 外部環境に依存しないデータ構造、規則、前提を表す |

`User` や `Context Loop` は、一つのクラスへ直接対応する概念ではありません。
複数の型が作る実行経路全体によって表現されます。

README とアーキテクチャ図の `Storage` は、メモリ上の一時データから
DB やファイルシステムの永続データまでを含む広い概念です。
ライブラリでは、この概念を、メモリ上の値を所有する `IStoragePort` / `Storage`、
外部保存先と読み書きする `IPersistencePort`、保存形式を変換する `ISerializerPort`
へ分割して実装しています。

## Context Loop がライブラリ上で実行されるまで

ライブラリ上の主要な呼び出しは、次の方向へ進みます。

```text
Program
  ├─ 実行環境を Builder へ登録する
  ├─ Context を作成または再利用する
  └─ SystemFlowHandler.ExecuteAsync(context)
       └─ SystemFlow.ExecuteAsync(context)
            └─ Interaction.ExecuteAsync(context)
                 ├─ Operation Port
                 ├─ Storage Port
                 ├─ Silent External Port
                 └─ Reaction Port
                      └─ Context の更新と User が観測する反応
```

一回の Interaction が完了しても、Context Loop 全体が終了するとは限りません。
SystemFlow は、現在の Context と Interaction の結果を見て、次の Interaction を実行するか、
SystemFlow を終了するかを決めます。

```text
Current Context
    ↓
Interaction を実行
    ↓
Context を参照・更新
    ↓
SystemFlow が継続条件を判断
    ├─ 継続する → 次の Interaction
    └─ 終了する → FlowEndToken を呼び出し側へ返す
```

Context Loop は専用の `ContextLoop` クラスによって実装されているわけではありません。
Context を再利用しながら Interaction を構成する SystemFlow の処理が、実行モデルとしての
Context Loop になります。

## Hello Door で見る一周の流れ

`InteractionFlow.Samples.HelloDoor` は、一つの Interaction を繰り返す最小の SystemFlow です。

```text
DoorState を持つ Context
    ↓
OperateDoor
    ├─ IDoorOperation から DoorCommand を受け取る
    └─ IDoorReaction が DoorState を更新し、結果を表示する
    ↓
DoorSystemFlow が ExitRequested を確認する
    ├─ false: OperateDoor をもう一度実行する
    └─ true : SystemFlow を終了する
```

### 1. Program が実行環境を選ぶ

`Program` は、どの Port 実装を使用するかを Builder へ登録します。

```csharp
var builder = new ScopeBuilder();

builder
    .Apply(ConsoleBuilder.Profile)
    .UseFunction<IDoorOperation, ConsoleDoorOperation>()
    .UseFunction<IDoorReaction, ConsoleDoorReaction>()
    .UseInteraction<OperateDoor>();
```

ここでは、`OperateDoor` 自身に Console 依存を書き込みません。
Console を使用するという選択は、実行環境を組み立てる `Program` に置かれています。

ソース:

- [`Program.cs`](./InteractionFlow.Samples.HelloDoor/Program.cs)
- [`ConsoleBuilder.cs`](./InteractionFlow.Standard/Console/Builders/ConsoleBuilder.cs)

### 2. Program が Context を準備する

Hello Door では、基本 Context に `DoorState` を重ねます。

```csharp
using var context = new ScopedFlowContext(new FlowContext())
    .With(new DoorState { IsOpen = false });
```

`DoorState` は、ドアが現在開いているかと、User が終了を要求したかを保持します。
同じ Context を次の `OperateDoor` に渡すことで、前回までの相互作用が次の反応へ影響します。

これが README で説明している、

> Interaction が Context を形作り、Context が次の Interaction を形作る

という循環の、Hello Door における実装です。

ソース:

- [`FlowContext.cs`](./InteractionFlow.Core/Entities/Contexts/FlowContext.cs)
- [`ScopedFlowContext.cs`](./InteractionFlow.Core/Entities/Contexts/ScopedFlowContext.cs)
- [`DoorState.cs`](./InteractionFlow.Samples.HelloDoor/Entities/DoorState.cs)

### 3. Handler を介して SystemFlow を実行する

登録済みの依存オブジェクトからスコープと SystemFlow を構築します。

```csharp
using var scope = builder.BuildScope();
using var flow = scope.BuildSystemFlow<DoorSystemFlow, IFlowContext>();

await flow.ExecuteAsync(context);
```

`flow` の実体は `SystemFlowHandler<IFlowContext>` です。
Handler は生成された `DoorSystemFlow` と、その依存オブジェクトを保持するスコープを一体で管理します。

Handler は Context を所有しません。
Context の作成、再利用、破棄は、Context を準備した呼び出し側が管理します。

### 4. SystemFlow が Interaction の継続を決める

`DoorSystemFlow` は `OperateDoor` を繰り返し、Context の終了要求を確認します。

```csharp
while (true)
{
    end = await operateDoor.ExecuteAsync(context);

    if (context.TryGet<DoorState>(out var door) &&
        door.ExitRequested)
    {
        break;
    }
}
```

このコードが Hello Door の Context Loop 本体です。

SystemFlow は Console 入出力を直接行いません。
どの Interaction を、どの順序で、どの Context 条件まで実行するかを構成します。

ソース:

- [`DoorSystemFlow.cs`](./InteractionFlow.Samples.HelloDoor/SystemFlows/DoorSystemFlow.cs)
- [`SystemFlow.cs`](./InteractionFlow.Core/SystemFlows/SystemFlow.cs)

### 5. Interaction が Function Port を組み合わせる

`OperateDoor` は、Operation の結果を Reaction へ渡します。

```csharp
protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
{
    var command = await operation.ReadCommandAsync(context);
    return await reaction.ReactAsync(context, command);
}
```

`OperateDoor` が依存するのは `IDoorOperation` と `IDoorReaction` です。
Console の読み書き方ではなく、「ドアをどう操作し、どう反応させるか」という意味を組み合わせます。

ソース:

- [`OperateDoor.cs`](./InteractionFlow.Samples.HelloDoor/Interactions/OperateDoor.cs)
- [`Interaction.cs`](./InteractionFlow.Core/Interactions/Interaction.cs)

### 6. Operation が User の操作を受け取る

`IDoorOperation` は、User の操作を `DoorCommand` として受け取る Port です。
Hello Door の実行構成では、`ConsoleDoorOperation` が Console 入力をこの契約へ接続します。

```text
User の Console 入力
    ↓
ConsoleDoorOperation
    ↓
IDoorOperation が表す DoorCommand
    ↓
OperateDoor
```

Interaction は Console を直接参照しないため、別の入力環境を使う場合も
`IDoorOperation` の実装を差し替えられます。

### 7. Reaction が Context の変化を観測可能にする

`ConsoleDoorReaction` は `DoorCommand` と現在の `DoorState` を参照し、
ドアの状態を更新すると同時に結果を Console へ表示します。

```text
DoorCommand + Current DoorState
    ↓
ConsoleDoorReaction
    ├─ DoorState を更新する
    └─ User へ結果を表示する
    ↓
Updated DoorState
```

README では、Reaction を

> Operation に対する Context の更新を観察可能な形で実行する

ものとして設計しています。

現在のライブラリは、この対応を Reaction の設計原則として表現しています。
ただし、実際に User が観測したか、Context 更新が必ず Reaction 内で行われたかを、
型や Analyzer が完全に検証するわけではありません。

ソース:

- [`IDoorReaction.cs`](./InteractionFlow.Samples.HelloDoor/ExternalPorts/ReactionPorts/IDoorReaction.cs)
- [`ConsoleDoorReaction.cs`](./InteractionFlow.Samples.HelloDoor/Externals/Reactions/ConsoleDoorReaction.cs)

## Context の実装

### IFlowContext が提供する最小契約

`IFlowContext` は、Context の具体的なデータ構造を固定しません。

```csharp
public interface IFlowContext
{
    CancellationObject Cancellation { get; }

    bool TryGet<T>(out T value);
}
```

提供するのは次の二つです。

- その Context に紐づくキャンセル制御
- 型を指定して文脈値を取得する仕組み

Interaction は、特定の Context 実装へキャストする代わりに、
原則として `TryGet<T>` により必要な文脈値を要求できます。

`TryGet<T>` は、要求型の値が存在しない場合に `false` を返します。
ただし、あらゆる失敗を `false` に変換する API ではありません。
`ScopedFlowContext` では、Entry の循環参照など、単なる値の不在や型不一致ではない
解決失敗を検出した場合は例外を送出します。
破棄済みの `ScopedFlowContext` に対する呼び出しも `ObjectDisposedException` になります。

### Context に置く値の読み取りと更新

`IFlowContext` の基本契約は `TryGet<T>` による読み取りです。
任意の値を置換する `Set<T>` や、値を削除する `Remove<T>` は定義していません。

その意味で、Context が公開する値のアクセスは基本的に ReadOnly です。
ただし、これは取得した参照型オブジェクトの内部状態まで不変にするという意味ではありません。
Hello Door の `DoorState` のような mutable な参照型を取得した場合、そのプロパティは更新できます。

値そのものを置き換える必要がある場合は、`RefEntry<TValue>` を Context に追加できます。
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

同じ Context から `TryGet<int>` を呼ぶと、`RefEntry<int>` の内側にある現在値を取得できます。
ラッパー自体が必要な場合は `TryGet<RefEntry<int>>` を使用し、その `Value` を更新します。

### FlowContext

`FlowContext` は `CancellationObject` を持つ最小実装です。
基本実装の `TryGet<T>` では、この `CancellationObject` 自身を文脈値として取得できます。

`FlowContext` 自体が、アプリケーション固有の状態や「関係の歴史」を自動的に保存するわけではありません。
呼び出し側が Context を再利用し、その Context から取得できる値が更新され続けることで、
過去の相互作用が次の相互作用へ引き継がれます。

### ScopedFlowContext

`ScopedFlowContext` は、親 Context の値を参照しながら、一時的な値を追加します。

```text
ScopedFlowContext
  ├─ 新しく追加された値を新しい順に探索する
  └─ 見つからなければ親 Context を探索する
```

同じ型の値を複数追加した場合は、後から追加された値が先に見つかります。
追加値が `Entry` の場合は、Entry が保持する値も再帰的に探索されます。

`ScopedFlowContext.Dispose` は、追加値を破棄しません。
内部の探索リストを解放し、追加値の操作と探索を利用不能にします。
破棄後に `With` または `TryGet<T>` を呼ぶと `ObjectDisposedException` が発生します。
`Cancellation` は探索リストを使用せず親 Context へ委譲するため、破棄後も取得できます。

追加値が `IDisposable` であり、破棄が必要な場合、その値の所有者が別途破棄する必要があります。

### CancellationObject のライフサイクル

`CancellationObject` は、キャンセル要求に使用するトークンと、
キャンセル時に完了を待つタスクを Context 単位で管理します。

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
同じ Context を次の処理へ再利用できる
```

`Cancel()` はタスクを強制終了しません。
キャンセル対象の処理が `GetToken()` で取得したトークンを監視し、
キャンセル要求へ応答する必要があります。

標準の `CancellationHandling` がキャンセル処理後に状態をリセットするのは、
一度キャンセルされた Context を
永久にキャンセル済みのままにせず、Context Loop の次の処理へ再利用できるようにするためです。

### ネストした Interaction の Context

`Interaction.NestedExecuteAsync` は、親 Context の文脈値を参照できる内部 Context を作成します。
この内部 Context は親とは別の `CancellationObject` を持ち、親のキャンセル要求と連動します。

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

したがって、すべての Context が常に同じ `CancellationObject` を共有するわけではありません。

- `ScopedFlowContext` は親の `CancellationObject` を共有する
- ネスト実行用 Context は独立したキャンセル制御を持ち、親キャンセルと連動する

という違いがあります。

ネストした Interaction が未解決の例外またはキャンセルを含む `ReactionEnd` を返した場合、
`NestedExecuteAsync` はその例外を親 Interaction へ再送出します。
これにより、親 Interaction の `ExecuteAsync` が、親側の Exception Port または
Cancellation Port を通じて結果をもう一度処理できます。

### Context の所有権

`SystemFlow.ExecuteAsync` と `Interaction.ExecuteAsync` は、渡された Context を破棄しません。
`FlowEndToken` も Context の所有権を引き受けません。

Context の寿命は、次のように考えます。

```text
Context を作成した呼び出し側
  ├─ SystemFlow へ渡す
  ├─ 必要なら次の SystemFlow へ再利用する
  └─ 不要になった時点で破棄する
```

## SystemFlow の実装

`ISystemFlow<TContext>` は、指定した Context 型で SystemFlow を実行する契約です。

```csharp
Task<FlowEndToken> ExecuteAsync(TContext context);
```

`SystemFlow<TContext>` 基底クラスは、派生クラスの `ExecuteCoreAsync` を実行し、
返された Interaction の終了結果を SystemFlow に渡された Context へ結び直します。

派生 SystemFlow が主に決めるのは次の内容です。

- どの Interaction を使用するか
- どの順序で実行するか
- Context のどの情報を継続条件として使うか
- どの時点で SystemFlow を終了するか

SystemFlow 基底クラスがループや分岐方法を固定しているわけではありません。
逐次実行、条件分岐、繰り返し、別の SystemFlow の実行などを、
派生クラスがユーザー体験に合わせて構成します。

## Interaction の実装

`Interaction` は、Function Port を組み合わせて一回の相互作用を実行する基底クラスです。

派生クラスが実装する中心処理は次のメソッドです。

```csharp
protected abstract Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context);
```

基底クラスは、その周囲で次の共通処理を行います。

- 実行前のキャンセル確認
- キャンセル対象タスクの登録
- `OperationCanceledException` の Cancellation Port への委譲
- その他の例外の Exception Port への委譲
- `ReactionEnd` と実行時 Context から `FlowEndToken` を作成

これにより、派生 Interaction は例外表示やキャンセル表示の実装を直接持たず、
Interaction 固有の Function の組み合わせへ集中できます。

ただし、基底クラスは「Interaction が必ず Operation を呼ぶこと」や
「User に観測可能な Reaction が実際に行われたこと」まで検証しません。
それらは Port の設計、実装、Analyzer、コードレビューによって維持するアーキテクチャ上の規約です。

## Function Port と Function External

### Function Port

Function Port は、Interaction から見える外部機能の契約です。

ライブラリでは、Function を次の種類へ分類します。

| 種類 | Port | 意味 |
| --- | --- | --- |
| Operation | `IOperationPort` | User の操作や外部条件を受け取る |
| Reaction | `IReactionPort` | User が観測できる反応を提供する |
| Storage | `IStoragePort` | Context とは別の寿命で値を保持する |
| Silent External | `ISilentExternalPort` | User に直接観測されない外部環境と連携する |

Port は「Console へ表示する」「特定の DB へ保存する」といった実現方法ではなく、
Interaction が必要とする機能の意味を定義します。

### Function External

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

### 現在のレイヤーメタデータに関する注意

`FlowLayerTypes` には `FunctionPort` と `FunctionExternal` の両方があります。
一方、現在の `Operation`、`Reaction`、`Storage`、`SilentExternal` 基底実装は、
`IFlowNode.Layer` として `FunctionPort` を返します。

そのため、現時点で Port と External の区別を実際に表しているのは、主に次の二つです。

- `ExternalPorts` / `Externals` というソースと namespace の境界
- Analyzer のレイヤー依存規則

`IFlowNode.Layer` の実行時メタデータだけでは、両者を区別できません。
これはアーキテクチャ図と実装メタデータの対応を今後整理する必要がある点です。

### Function が保持する状態

Operation、Reaction、Storage、Silent External の Port は `IFlowNodeStateful` を継承し、
実装が保持するメモリ上の状態を明示的に初期化する `ForceResetMemoryState` を定義します。

この状態は Runtime Context とは異なります。

- Context は、Interaction 間で引き継ぐ文脈を表す
- Function の状態は、外部機能の実装が動作するために保持する設定やキャッシュを表す

`ForceResetMemoryState` は、通常の状態変更規則よりもリセットを優先する強い操作です。
スコープの破棄時に自動的に呼ばれるメソッドではないため、
明示的な再初期化が必要な場面で呼び出し側が使用します。

Function が `IHasFunctionState<TState>` を実装している場合は、
`FunctionStateScope<TState>` によって状態を一時的に差し替えられます。
スコープ作成時に現在状態をコピーし、破棄時に元の状態へ戻します。

```csharp
using var scope = consoleWrite.GetStateScope();
scope.State = state;

return await consoleWrite.Write(context, output);
```

この仕組みにより、一回の Function 呼び出しだけ表示設定などを変更し、
その一時設定を Runtime Context や後続の Interaction へ残さずに済みます。

## ReactionEnd と FlowEndToken

### ReactionEnd

`ReactionEnd` は、Reaction 系 API が確定した処理結果を表します。

保持するのは次の情報です。

- 未解決の例外があるか
- その例外がキャンセル例外か
- 実際の例外オブジェクト

`ReactionEnd` のコンストラクターは `internal` です。
通常のライブラリ利用側は任意の `ReactionEnd` を直接生成せず、
Reaction Port または Reaction 基底クラスの `GetEnd` を使用します。

Reaction 実装が成功結果を返す最小例は次のとおりです。

```csharp
public ValueTask<ReactionEnd> ReactAsync(IFlowContext context)
{
    // User が観測する反応や Context の更新を行う。
    return new(GetEnd());
}
```

未解決の例外を結果へ含める場合は、`GetEnd` に例外を渡します。

```csharp
return new(GetEnd(exception));
```

この構造は、Interaction の結果を Reaction 系の契約へ寄せるための制約です。
ただし、`ReactionEnd` が存在すること自体は、User が実際に反応を観測したことの証明ではありません。

### FlowEndToken

`FlowEndToken` は `ReactionEnd` と、そのフローの実行に渡された Context を結びつけます。

```text
FlowEndToken
  ├─ LastContext: そのフローの ExecuteAsync に渡された Context
  └─ End: ReactionEnd
```

`LastContext` は、結果が最初に生成された内部 Context の来歴を常に保持するものではありません。
たとえば SystemFlow は、内部 Interaction の `ReactionEnd` を、
SystemFlow 自身に渡された Context へ結び直して返します。
これは、SystemFlow の外側から見た `LastContext` を、内部実装で一時的に使った Context ではなく、
その SystemFlow の実行境界へ渡された Context に揃えるためです。

`FlowEndToken` もコンストラクターが `internal` であり、
通常のライブラリ利用側が直接生成するものではありません。
Interaction 基底実装では、Reaction が返した `ReactionEnd` を実行時 Context と結合します。

```csharp
private static FlowEndToken GetEnd(
    IFlowContext context,
    ReactionEnd reactionEnd)
{
    return IInteraction.GetEnd(context, reactionEnd);
}
```

`IInteraction.GetEnd` 内部での生成は、次の最小コードです。

```csharp
return new FlowEndToken(context, reactionEnd);
```

SystemFlow では、内部 Interaction の `End` を取り出し、
SystemFlow 自身に渡された Context と結合し直します。

```csharp
return new FlowEndToken(context, interactionEnd.End);
```

`FlowEndToken` は Context を破棄せず、所有権も取得しません。

### Result、ReactionEnd、FlowEndToken の役割

ライブラリでは、処理の範囲に応じて三種類の結果表現を使い分けます。

| 結果型 | 主な範囲 | 表すもの |
| --- | --- | --- |
| `Result` / `Result<TValue>` | Function や補助処理 | 局所処理の成功、成功値、または失敗 |
| `ReactionEnd` | Reaction / Interaction | Reaction 系 API が確定した Interaction の終了結果 |
| `FlowEndToken` | Interaction / SystemFlow 境界 | 終了結果と、その実行境界へ渡された Context |

`Result` の失敗が、自動的に `ReactionEnd` へ変換されるわけではありません。
Interaction や Reaction は、Function が返した失敗をどのように処理し、
User が観測できる反応や未解決例外へ変換するかを決めます。

`Result` には、次の基本的な性質があります。

- `default(Result)` は成功として扱われる
- `default(Result<TValue>)` は成功値を持たないため失敗として扱われる
- `Result<TValue>` の成功値に `null` は使用できない
- `Exception` 派生型の値は成功値ではなく、失敗として扱われる
- `Then` は成功時だけ次の処理へ進み、失敗はそのまま伝播する
- `ThenError` は失敗時だけ回復処理を実行する

`Result` は、送出済みのすべての例外を自動的に捕捉する仕組みではありません。
`OnSuccess` や `Then` へ渡した処理が例外を送出した場合、その例外は通常どおり呼び出し側へ送出されます。
失敗として伝播させる場合は、処理側が例外を失敗 `Result` へ変換する必要があります。

## Builder と実行スコープ

SystemFlow を実行するには、SystemFlow、Interaction、Port 実装、
例外処理、キャンセル処理などを組み立てる必要があります。

この構築を担当する主な型は次のとおりです。

| 型 | 役割 |
| --- | --- |
| `ScopeBuilder` | 登録情報から依存解決スコープを一つ構築する |
| `ScopeHandler` | 構築済みスコープを保持し、親スコープも含めて依存を解決する |
| `SystemFlowBuilder<TContext>` | SystemFlow と専用スコープを構築する |
| `SystemFlowHandler<TContext>` | SystemFlow の実行と専用スコープの寿命を管理する |

`ScopeBuilder` と `SystemFlowBuilder<TContext>` は、Build 後に内部のサービス登録を解放します。
同じ Builder へさらに登録したり、もう一度 Build したりすることはできません。
別のスコープを構築する場合は、新しい Builder を作成します。

`Use`、`UseInteraction`、`UseFunction` による標準登録は scoped ライフタイムです。
同じスコープ内では同じインスタンスが解決され、別のスコープでは別のインスタンスになります。
解決ごとに新しいインスタンスが必要な補助サービスには `UseTransient` を使用します。

`ScopeHandler.BuildSystemFlow` 拡張メソッドは、既存の `ScopeHandler` を親として
`SystemFlowBuilder<TContext>` を使用します。

```text
Global Scope
  ├─ 共有する Function
  └─ 共有する Interaction
        ↑ 親として参照
SystemFlow Scope
  ├─ SystemFlow
  └─ SystemFlow 固有の依存
```

子スコープで解決できない依存は、指定された親スコープから順に探索されます。
同じサービスを子と親の両方が提供する場合は、子スコープのインスタンスが優先されます。
複数の親が提供する場合は、`parents` に指定された順序で最初に見つかったインスタンスを使用します。
子スコープを破棄しても親スコープは破棄されません。

`SystemFlowHandler.Dispose` は、Handler が保持する SystemFlow 用スコープを破棄し、
以降の実行を無効にします。

Builder の詳細は、[SystemFlow Builder の詳細](./docs/SystemFlowBuilder.md) も参照してください。

## 実行時の依存ノードツリー

SystemFlow、Interaction、Function Port 実装は `IDependencyNode` として、
実行時に依存しているノードのインスタンスを `Dependency` に保持します。

たとえば Hello Door の `OperateDoor` は、コンストラクターでは
`IDoorOperation`、`IDoorReaction`、例外 Port、キャンセル Port という契約を受け取ります。
Builder が依存を解決した後、`Dependency` に入るのは、
それらの Port 契約へ割り当てられた実行時の具体的なインスタンスです。

Hello Door の標準構成では、概念的に次の実行時ツリーになります。

```text
DoorSystemFlow
  └─ OperateDoor
       ├─ ConsoleExceptionHandling
       ├─ ConsoleCancellationHandling
       ├─ ConsoleDoorOperation
       └─ ConsoleDoorReaction
```

このツリーの `ConsoleDoorOperation` などは、説明上 Port 名へ置き換えたものではなく、
DI によって実際に生成され、SystemFlow と Interaction が参照している実行時の型です。

Port と実行時型の対応は次のようになります。

| Port 契約 | Hello Door で解決される実行時型 |
| --- | --- |
| `IExceptionPort<Exception>` | `ConsoleExceptionHandling` |
| `ICancellationPort` | `ConsoleCancellationHandling` |
| `IDoorOperation` | `ConsoleDoorOperation` |
| `IDoorReaction` | `ConsoleDoorReaction` |

`SystemFlowHandler.Root` は、生成された SystemFlow の実行時インスタンスを
`IDependencyNode` として公開します。
`DependencyTreeView.GetDependencyTreeText` を使うと、そのインスタンスから
実際の依存ノードを再帰的に辿って表示できます。

```csharp
var treeText =
    DependencyTreeView.GetDependencyTreeText(flow.Root);
```

表示名には各実行時インスタンスの `ToString()` が使われます。
そのため、`ToString()` を上書きしていない型は、通常は namespace を含む
実行時の型名として表示されます。上の図では読みやすさのため短い型名で表記しています。

依存ノードツリーは DI コンテナの登録一覧そのものではありません。
実行する SystemFlow を根として、現在組み立てられている SystemFlow、
Interaction、Function の実体を観察するための構造です。

`InteractionFlowArchitecture002` は、コンストラクターで実際に受け取った依存ノードが
この実行時ツリーから欠落しないように、`Dependency` または基底コンストラクターへ
渡されていることを検査します。

## ライブラリが保証することと設計原則

Interaction Flow Architecture のすべての意味が、C# の型だけで完全に保証されるわけではありません。

| 内容 | 現在の実装 |
| --- | --- |
| SystemFlow が受け取る Context 型 | ジェネリック型制約と API が規定する |
| Interaction の正常完了時の戻り値が `FlowEndToken` である | `IInteraction` のメソッドシグネチャが規定する |
| Interaction 基底クラスが例外とキャンセルを Port へ委譲する | `Interaction` 基底実装が提供する |
| `ReactionEnd` の直接生成を一般利用側へ公開しない | `internal` コンストラクターが制限する |
| Context の値を型で取得する | `IFlowContext.TryGet<T>` が提供する |
| Port と External の namespace 依存方向 | Analyzer が有効な場合に検査する |
| Reaction が必ず User に観測された | 型では保証しない |
| Context 更新が必ず Reaction 内だけで行われる | 現在は保証しない |
| SystemFlow が必ず例外・キャンセル時に終了する | 派生 SystemFlow の継続条件に依存する |
| Context が「関係の歴史」として適切に設計されている | 設計とレビューに依存する |
| namespace 名を使わずに意味的なレイヤーを完全判定する | 現在の Analyzer では保証しない |

Exception Port が例外を再送出する設定の場合や、例外・キャンセル処理自体が例外を送出した場合、
`Interaction.ExecuteAsync` は `FlowEndToken` を返さず、例外が呼び出し側へ伝播します。

型による制約は、設計判断を不要にするためのものではありません。
README と Philosophy が示す「User と System の関係」をコード上でも追えるようにするための支援です。

## Context Loop を支える補助機構

ここまでの型が Context Loop の主要な実行経路です。
ライブラリは、実用的な Context やデータ保持を支えるため、さらに次の機構を提供します。

### Entry

`Entry<TValue>` は、値をラップし、要求された型として値を解決するための基底クラスです。

保持値が別の Entry である場合は再帰的に解決します。
循環する Entry 参照を検出した場合は、失敗結果を返します。

`ScopedFlowContext` は、追加値を内部の Entry として保持することで、
値そのものだけでなく、追加された Entry の内側の値も探索できます。

`RefEntry<TValue>` は setter を公開し、外部から保持値を更新できる Entry です。

Entry は Context Loop そのものではありません。
Context や Storage に置く値へ、型解決や更新可能性などの扱いを与える補助機構です。

### Storage

`IStoragePort` は、メモリ上に生成した値をキー単位で保持する Function Port です。

```text
Context
  └─ Storage のキーを提供する
        ↓
Storage
  ├─ 既存値を取得する
  ├─ 値がなければ作成する
  └─ 値を削除し、必要なら破棄する
```

`Storage<TKey, TValue>` は、Dictionary を使う外部副作用に依存しない既定基底実装です。
`GetOrCreate` で作成して登録した値は、Storage が所有するメモリ上の値として扱われます。

#### Context から Storage のキーを取得する

`IStoragePort<TKey>.GetKey(IFlowContext)` は、
現在の Context からその Storage が使用するキーを取得する契約です。

既定の `Storage<TKey, TValue>` は `context.TryGet<TKey>` により、
Context から `TKey` 型の値を直接取得します。
キーを取得できない場合は、`KeyNotFoundException` を含む失敗 `Result<TKey>` を返します。

派生 Storage は `GetKey` をオーバーライドし、複数の文脈値からキーを組み立てたり、
現在の Context でそのキーを使用できるか検証したりできます。
これにより、Interaction は Storage 固有のキー導出処理を直接持たず、
Context を渡して Storage に判断を委譲できます。

Storage は `IReadOnlyCollection<KeyValuePair<TKey, TValue>>` として列挙され、
`Get` / `GetOrCreate` も値を取得する API です。
任意の値を直接登録または置換する汎用 `Set` API は公開していません。

その意味で、Storage の値への基本アクセスも ReadOnly です。
ただし、Storage 自体には値の作成、削除、クリアというライフタイム操作があります。
また、Storage が保持する `TValue` が mutable な参照型であれば、その内部状態は更新できます。

値そのものを明示的に差し替える Storage が必要な場合は、
`TValue` に `RefEntry<T>` を使用できます。

```csharp
if (storage.GetOrCreate(key).Try(out var entry, out _))
{
    entry.Value = newValue;
}
```

この場合も Storage のキーと Entry の登録関係は Storage が管理し、
Entry が保持する現在値だけを `RefEntry.Value` によって更新します。

#### 作成・削除可能性を Result で制御する

Storage の作成系・削除系 API が `Result` を返すのは、
失敗を例外として包むためだけではありません。
派生 Storage が、どのキーと値を作成または削除できるかを制御するためでもあります。

`Storage<TKey, TValue>` は、派生クラスに次の二つの判断を委譲します。

```csharp
protected abstract Result<TValue> CreateNewValue(TKey key);

protected abstract Result CanRemoveValue(TKey key, TValue value);
```

- `CreateNewValue` が成功した場合だけ、新しい値が Storage に登録される
- `CanRemoveValue` が成功した場合だけ、値の登録解除と必要な破棄が実行される
- `CreateNewValue` または `CanRemoveValue` が失敗 `Result` を返した場合は、
  その失敗が呼び出し側へ返り、Storage の登録状態は変更されない
- `ClearAndDispose` / `ClearWithoutDispose` も、すべての値を削除可能と確認してからクリアする

たとえば、`counter:` で始まるキーだけを作成でき、
カウンターが `0` のときだけ削除できる Storage は次のように実装できます。

```csharp
private sealed class CounterStorage
    : Storage<string, RefEntry<int>>
{
    protected override Result<RefEntry<int>> CreateNewValue(string key)
    {
        if (!key.StartsWith("counter:", StringComparison.Ordinal))
        {
            return new ArgumentException(
                "Only counter keys can be created.",
                nameof(key));
        }

        return new RefEntry<int>(0);
    }

    protected override Result CanRemoveValue(
        string key,
        RefEntry<int> value)
    {
        if (value.Value != 0)
        {
            return new InvalidOperationException(
                "A non-zero counter cannot be removed.");
        }

        return Result.Success;
    }
}
```

この例では、許可されていないキーの `GetOrCreate` は失敗結果になり、値は登録されません。
また、取得した `RefEntry<int>` の値が `0` 以外であれば、
`RemoveAndDispose` と `RemoveWithoutDispose` は失敗し、登録済みの値を保持します。

`Result` は、作成処理、削除可否判定、値の破棄中に送出された例外を
自動的に失敗結果へ変換するものではありません。
たとえば `RemoveAndDispose` は、登録を外した後に値を破棄します。
その `Dispose` が例外を送出した場合、値は登録から外れた状態で例外が呼び出し側へ伝播します。

`ClearAndDispose` は、すべての値を削除可能と確認した後、値を順に破棄してから
登録をクリアします。途中の `Dispose` が例外を送出した場合はその時点で処理が中断され、
登録はクリアされません。そのため、一部の値が破棄済みのまま Storage に残る可能性があります。
破棄例外があっても登録状態を必ずクリアする必要がある場合は、
後述する `ForceResetMemoryState` の動作との違いに注意してください。

#### 通常削除と強制リセット

`RemoveAndDispose`、`RemoveWithoutDispose`、Clear 系は、
派生 Storage の `CanRemoveValue` を尊重する通常のライフタイム操作です。

一方、`ForceResetMemoryState` は、Storage の登録状態を強制的に初期化する操作です。

- `CanRemoveValue` による削除可否判定を行わない
- 破棄可能な値をすべて破棄する
- 複数の破棄例外を `AggregateException` へ集約する
- 破棄例外があっても、最後に登録状態をクリアする

通常の削除規則よりもメモリ状態のリセットを優先するため、
フローや実行環境を明示的に再初期化する必要がある場面で使用します。

削除時には次の動作を選択できます。

- `RemoveAndDispose`: 登録を外し、破棄可能な値を破棄する
- `RemoveWithoutDispose`: 値を破棄せず、登録だけを外す

Storage は Runtime Context そのものではありません。
Context が「現在どの値を必要としているか」を示し、
Storage はその値を Context とは別の寿命で保持します。

### PersistentEntry

`PersistentEntry<TPersistenceId, TValue>` は、メモリ上の値と永続化先 ID を関連付けます。

```text
PersistentEntry
  ├─ PersistenceId
  └─ Current Value
        ↕
IPersistencePort
```

`Load` と `DeleteAndReset` は Entry 自身にあり、
`Save` と `Exists` は拡張メソッドとして提供されます。

これらの操作は、Persistence 側の処理に成功した場合だけ
Entry が保持するメモリ上の状態を確定します。

- `Load` は読み込み成功時だけ `Value` を読み込んだ値へ置き換える
- `Load` に失敗した場合は、`Value` プロパティを別の参照へ置き換えない
- `DeleteAndReset` は削除成功時だけ `Value` を `default` へ戻す
- `Save` は `Value` が `null` の場合、Persistence を呼び出さず失敗結果を返す
- `Exists` は Entry の現在値を変更しない

ただし、`Load` は現在の値を `oldValue` として Persistence や Serializer へ渡します。
それらの実装が既存オブジェクトを再利用して内部状態を変更した後に失敗した場合、
`Value` の参照は維持されても、オブジェクト内部の変更まで自動的にロールバックされるわけではありません。

`Load` による置き換えと `DeleteAndReset` は、
置き換え前の値を自動的には破棄しません。
古い値が `IDisposable` であり、別途破棄が必要な場合は、
その値の所有方針を呼び出し側または派生 Entry が決める必要があります。

### Persistence

`IPersistencePort<TPersistenceId, TValue>` は、ID で識別される値について、
次の操作を定義します。

- `Save`
- `Load`
- `Delete`
- `Exists`
- `GetAllIds`

Persistence は保存先との読み書きを担当します。
メモリ上の値を作成し、複数の Interaction 間で所有する責務は Storage 側にあります。

Persistence Port は、値や既存値を `Result` のまま受け取ります。

```csharp
Task<Result> Save(
    TPersistenceId id,
    Result<TValue> value);

Task<Result<TValue>> Load(
    TPersistenceId id,
    Result<TValue> oldValue);
```

これにより、呼び出し元で発生した失敗をすぐに例外として送出せず、
Persistence やその下位の Serializer まで結果パイプラインとして渡せます。
各実装は、失敗をそのまま伝播するか、回復処理を行うかを選択できます。

`oldValue` は、読み込み前に Entry が保持していた値です。
Persistence や Serializer は、読み込んだ内容から常に新しいオブジェクトを作るだけでなく、
既存値を参照または再利用して内容を更新できます。

Persistence と Serializer は `IFlowNode` として Function 種別を持つ Port ではなく、
Storage / Persistence 機構を構成する `IFlowSubNode` として依存ノードグラフへ参加します。
サンプルの構成によっては、Interaction が Storage Port と Persistence Port の両方を受け取り、
それぞれの実行時インスタンスを直接の依存ノードとして列挙することもあります。

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

Serializer の入力と参照値も `Result` で渡されます。

```csharp
Task<Result<TData>> Serialize(
    Result<TValue> inputValue,
    Result<TData> refData);

Task<Result<TValue>> Deserialize(
    Result<TData> inputData,
    Result<TValue> refValue);
```

- `inputValue` / `inputData`: 変換対象と、その取得失敗を表す
- `refData`: 既存 Stream など、書き込み先として参照・再利用するデータを表す
- `refValue`: 既存オブジェクトなど、復元先として参照・再利用する値を表す

参照値を実際に再利用するか、新しい値を生成するかは Serializer 実装が決めます。
`Result` を受け取ることで、入力取得の失敗も変換処理の失敗も、
同じ結果パイプラインで呼び出し側へ戻せます。

Storage、PersistentEntry、Persistence、Serializer の責務は次のように分かれます。

| 要素 | 責務 |
| --- | --- |
| Storage | メモリ上の値を作成・保持・破棄する |
| PersistentEntry | 値と永続化先 ID を関連付ける |
| Persistence | 保存先との読み書きを行う |
| Serializer | 値と保存形式を変換する |

## Analyzer による設計支援

`InteractionFlow.Analyzers` は、アーキテクチャの境界をコード編集中に確認するための
Roslyn Analyzer です。

現在は主に次の二つを検査します。

### レイヤー依存関係

`InteractionFlowArchitecture001` は、namespace のセグメントに含まれるレイヤー名から
所属レイヤーを判定し、依存方向を検査します。
複数の対象レイヤー名を含む場合は、namespace の先頭から見て最初に一致した
セグメントを所属レイヤーとして扱います。

対象となる主な namespace 名は次のとおりです。

- `SystemFlows`
- `Interactions`
- `ExternalPorts`
- `Externals`
- `Builders`
- `Entities`

たとえば、次のような依存を検査できます。

- `SystemFlows` から `Interactions` への依存
- `Interactions` から `ExternalPorts` への依存
- `Externals` から `ExternalPorts` への依存
- 各レイヤーから `Entities` への依存

一方、`Interactions` から `Externals` への直接依存などは診断対象になります。

### 依存ノード宣言

`InteractionFlowArchitecture002` は、`IDependencyNode` を実装するクラスについて、
コンストラクターで受け取った依存ノードが依存グラフから欠落していないかを検査します。

主な検査内容は次のとおりです。

- 依存引数が `Dependency` に含まれているか
- 基底クラスも `IDependencyNode` の場合、依存引数が基底コンストラクターへ渡されているか
- 非 `sealed` クラスが、派生クラス用の `params IDependencyNode[] dependency` を受け取れるか

### Analyzer の適用範囲

Analyzer は、設計の意味を完全に証明するものではありません。

- `interactionflow_enabled = True` の場合に有効になる
- namespace に対象レイヤー名を含まない型は、レイヤー依存検査の管理対象外になる
- Context 更新と Reaction の意味的な対応は検査しない
- 複雑な依存ノードの組み立てを完全には追跡しない

このリポジトリでは、ルートの `.editorconfig` で Analyzer を有効にし、
診断モードを `Error` に設定しています。

詳細は [`InteractionFlow.Analyzers/README.md`](./InteractionFlow.Analyzers/README.md) を参照してください。

## パッケージ境界との対応

ライブラリの実装は、次のプロジェクトへ分けられています。

### InteractionFlow.Core

アーキテクチャ概念と、外部の利用形態に依存しない基本契約・振る舞いを置きます。

主な内容:

- Context
- SystemFlow / Interaction の契約と基底実装
- Function Port
- Entry / Result
- Storage の外部副作用に依存しない基底実装
- Builder / Handler の抽象とライフタイム契約

### InteractionFlow.Standard

Core の契約を、一般的な .NET アプリケーションで利用しやすい実装へ接続します。

主な内容:

- Microsoft.Extensions.DependencyInjection を使用する Builder
- Console の標準 Port 実装
- FileSystem Persistence
- Stream / Text Serializer

### InteractionFlow.Samples.*

Core と Standard を具体的な Context Loop として組み立て、
API の使い方と責務分割を検証します。

- `HelloDoor`: 一つの Interaction を繰り返す最小例
- `Parrot`: 複数 SystemFlow、スコープ付き Context、Storage の例
- `Notepad.Core`: アプリケーション中心部の Entity / Port / Interaction / SystemFlow
- `Notepad`: FileSystem、Serializer、Console を接続する実行構成
- `Notepad.Secure`: Port 実装と実行構成を差し替える例

詳細は [Core / Standard / Samples の役割](./docs/RoleOfMainProjects.md) を参照してください。

## 現在の不完全な点と将来の改善候補

ここまでに説明した実装は、現在の Architecture を利用できる形にしたものですが、すべての設計意図を型や Analyzer で強制できているわけではありません。また、比較的新しい概念には、既存実装との統合をさらに進められる部分があります。

以下は現時点の既知の課題と改善候補です。将来の互換性を約束する Roadmap ではなく、実際の利用例とサンプルで妥当性を確認しながら判断する設計上の検討事項です。

### Context の更新を Reaction に限定する

Architecture では、Interaction の最後を Reaction とし、User への反応と Context の更新を対応させます。一方、現在の `IFlowContext` は値の取得だけを定義しているものの、取得した参照型の値は Operation、Interaction、SystemFlow からも変更できます。そのため、「Context の更新は Reaction だけが行う」という規則は設計上の原則であり、型による制約にはなっていません。

現在は実装の自由度を優先しています。将来、既存サンプル、ネストした Interaction、例外処理、キャンセル処理、Storage との連携を含めても支障がないと十分に確認できれば、Reaction だけに更新能力を渡す仕組みや、その他のレイヤーには読み取り専用の Context を公開する仕組みを検討できます。

この制約を導入できれば、Context の変化が必ず Reaction を経由し、「最後はかならず反応で終わる」という設計をコード上でも保証しやすくなります。

### Entry が保持する値の破棄規則を統一する

`Entry<TValue>.Dispose` は、保持値が `IDisposable` の場合にその値を破棄します。Storage には、値を破棄して削除する API と、破棄せず登録だけを外す API の両方があります。一方、`ScopedFlowContext` の破棄は追加した Entry を参照範囲から外すだけで、Entry やその保持値を破棄しません。

これは、Entry が比較的新しく追加された概念であり、値の所有権と寿命の規則がすべての利用箇所で統一されていないためです。今後は、少なくとも次の点を明確にする必要があります。

- Entry が保持値を所有する場合と、参照だけを借用する場合の区別
- `RefEntry` の値を置き換えたとき、以前の値を誰が破棄するか
- `PersistentEntry` と Storage のどちらが保持値の寿命を管理するか
- Context のスコープ終了時に、スコープ内の Entry と保持値をどう扱うか
- 重複した `Dispose` や複数の Entry から同じ値を参照する場合の扱い

これらを整理したうえで、所有権を型や API 名で表現するか、すべての Entry に共通の破棄規則を持たせるかを検討します。

### ReactionEnd を Result ベースへ統合する

現在の `ReactionEnd` は、成功または `Exception` を保持する専用型です。一方、Storage、Persistence、Serializer など、ライブラリ内の多くの処理結果は `Result` / `Result<T>` で表現されています。

`ReactionEnd` は比較的新しい設計変更で追加されたため、共通の Result モデルとの統合が完了していません。Result ベースにできれば、成功・失敗の変換、連結、例外情報の扱いを共通化でき、専用の判定処理を減らせる可能性があります。

ただし、移行時には次の性質を維持する必要があります。

- ReactionEnd は Reaction からのみ生成される
- キャンセルとその他の失敗を区別できる
- `FlowEndToken` が最後の Context と終了結果を結びつける
- 既存の例外処理 Port とキャンセル処理 Port の流れを複雑にしない

そのため、単純に `ReactionEnd` を `Result` へ置き換えるのではなく、Reaction 固有の生成制約を残したまま内部表現を共通化できるかを検証します。

### Function 系 Layer のメタ情報を整理する

現在の `IFlowNode` は、`FlowLayerTypes` と `FunctionPortTypes` をメタ情報として公開します。しかし、Function Port を実装する External は interface の既定実装を通じて Port 側のメタ情報を継承するため、実体が External でも `FunctionPort` として見える場合があります。実際に、`Operation`、`Reaction`、`Storage`、`SilentExternal` の基底実装も現在は `FunctionPort` を返します。

この状態では、依存ノードツリー上の役割と、C# の interface / class の実装関係が完全には分離されていません。改善案として、次の選択肢があります。

- メタ情報上では Port / External を分類せず、どちらも `Function` として扱う
- 現在の `FunctionPort` を `FunctionPortOrExternal` のような分類へ変更する
- interface の既定実装ではなく、属性によって各実装の Layer を明示する
- Port の契約上の分類と、実行時ノードの Layer を別のメタ情報として持つ

判断には、Analyzer の依存規則、依存ノードツリーの表示、Builder による登録、既存 Port / External の実装量をあわせて検証する必要があります。目標は分類を増やすことではなく、Architecture 上の Function と、コード上の Port / External の関係を誤解なく表現することです。

### SystemFlow の終了結果を安全に伝播する

現在の `SystemFlow` は、各 Interaction が返した `FlowEndToken` を継続、終了、または別の Interaction への分岐にどう反映するかを制約しません。また、SystemFlow 自身の処理で発生した例外を Reaction へ変換する仕組みも持ちません。

したがって現在は、ライブラリ利用側が各 Interaction の終了結果を確認し、失敗やキャンセルの後も処理を続けるのか、その結果を返して SystemFlow を終了するのかを明示する責務を持ちます。SystemFlow 内で例外が発生し得る処理は Interaction として分離し、Reaction を通して終了させるか、SystemFlow の外側で適切に処理する必要があります。

将来は、終了結果の継続・中断を明示できる合成 API、未確認の `FlowEndToken` を検出する Analyzer、SystemFlow の例外を Reaction へ接続する境界などを検討できます。ただし、SystemFlow が持つフロー選択の自由を損なわないことが前提です。

### 同一スコープ内の並行実行規則を定義する

現在の `SystemFlowHandler` は、同じ SystemFlow の複数回実行や同時実行を禁止しません。一方、Function State や Storage などの標準実装には、同一スコープ内で共有される可変状態があり、すべての実装が並行実行に対応しているわけではありません。

したがって現在は、ライブラリ利用側が同一の Scope、SystemFlow、Context、Function を並行利用してよいかを判断する責務を持ちます。明示的に並行実行へ対応した Function を使用する場合を除き、同一スコープ内では SystemFlow を逐次実行し、並行実行が必要な場合は独立した Scope と Context を構築するのが基本です。

将来は、この逐次実行モデルを正式な制約として明示・検査するか、Function State の分離や同期機構を導入して並行実行を正式に支援するかを検討します。

### Builder・Context・Cancellation の寿命を統一する

Entry の保持値だけでなく、Builder が生成する Scope、SystemFlow、Context が所有する `CancellationObject`、Function が保持する外部リソースにも寿命があります。現在は、これらすべてを一つの所有権モデルで表現しておらず、同期的な `IDisposable` を中心に管理しています。

したがって現在は、ライブラリ利用側が `ScopeHandler` と `SystemFlowHandler` を確実に破棄し、Context の再利用範囲を決め、キャンセル対象処理が完了またはリセットされたことを確認する責務を持ちます。非同期破棄が必要な外部リソースを利用する場合は、その External 側で寿命を明示的に管理する必要があります。

将来は、Builder、Context、Cancellation、Entry、Function を通した所有権規則を整理し、必要に応じて `IAsyncDisposable` を含む統一的なライフタイム管理を検討します。

### 依存構造を有向非巡回グラフとして検証する

将来、依存ノードについて、**循環を許可せず、分岐した依存関係の再合流を許可する
有向非巡回グラフ**を正式なモデルとすることを検討します。
たとえば、次のように `B` と `C` の両方が同じ `D` へ依存する構造を有効とするモデルです。

```text
A -> B -> D
 \-> C -> D
```

これは単純な木ではありません。
Root から依存を辿る表示はツリーとして読みながら、
同じノードへの再合流を共有依存として扱うモデルです。

現在の Analyzer は依存ノード宣言の完全性を検査しますが、グラフ全体の循環までは検査しません。また、`DependencyTreeView` は再合流したノードを経路ごとに表示し、循環を安全に打ち切る仕組みを持ちません。したがって現在は、ライブラリ利用側が依存構造に循環を作らず、再合流するノードの共有と寿命が意図したものであることを確認する責務を持ちます。

将来は、Analyzer または Builder による循環検出と、再合流を参照として表現できる依存ノード表示を検討します。

### Analyzer の適用範囲を明確にする

現在の Analyzer は namespace 名から Layer を判定し、`IDependencyNode` の宣言を構文上追跡します。そのため、規定の Layer 名を含まない namespace、複雑な依存ノードの組み立て、Context の更新元、SystemFlow における終了結果の扱いなどは、すべてを検査できるわけではありません。また、Analyzer の有効化と許可する namespace root はプロジェクト側の設定に依存します。

したがって現在は、ライブラリ利用側が Analyzer を正しく有効化し、規定の namespace 構造に従い、診断対象外の設計規則をコードレビューやテストで確認する責務を持ちます。Analyzer の診断がないことは、Architecture のすべての規則を満たすことと同義ではありません。

将来は、Layer 判定方法と設定方法の改善に加え、Context 更新、終了結果の未確認、依存構造の循環など、型だけでは保証できない規則をどこまで Analyzer で支援するかを検討します。

---

[README に戻る](./README.md)
