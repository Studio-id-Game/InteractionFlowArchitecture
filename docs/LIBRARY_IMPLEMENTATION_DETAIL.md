<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./PHILOSOPHY.md) |
[計算モデルとして](./COMPUTATIONAL_MODEL.md) |
[ライブラリ実装](./LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./LIBRARY_IMPLEMENTATION_DETAIL.md) |

---

# ライブラリ実装の詳細 <a id="implementation-details"></a>

このドキュメントでは、アーキテクチャのモデルが、具体的なライブラリとしてどのように実装されているかを説明します。

より詳細な API の定義や仕様については、各ソース内の XML ドキュメントコメントを参照してください。

## 実装の詳細 - 目次

- [システムの組み立て (System Flow Builder)](#system-flow-builder)
- [値と処理結果の意味論 (Result, ReactionEnd, FlowEndToken, Entry)](#entry-result)
- [文脈の構成 (Context)](#context)
- [機能の分離 (Functions)](#functions)
- [体験と相互作用のフロー (Interaction, SystemFlow)](#interaction-systemflow)

## システムの組み立て (System Flow Builder) <a id="system-flow-builder"></a>

`Program` は、実行環境を選び、依存オブジェクトを組み立て、`IFlowContext` インスタンスを渡して `SystemFlow` を実行します。

System Flow Builder の設計意図と、Builder、Handler、Scope に分けている理由については、
[System Flow Builder の設計](./SYSTEM_FLOW_BUILDER.md) を参照してください。

このライブラリでは、複数のSystemFlow間での依存スコープの共有などを想定して、
親子関係を持てる依存スコープを実装しています。

このスコープは、Builder によって解決・構築され、Handler によって寿命が管理されます。

| 型 | 役割 |
| --- | --- |
| `ScopeBuilder` | 登録情報から依存解決スコープを一つ構築する |
| `ScopeHandler` | 構築済みスコープを保持し、親スコープも含めて依存を解決する |
| `SystemFlowBuilder<TContext>` | SystemFlow と専用スコープを構築する |
| `SystemFlowHandler<TContext>` | SystemFlow の実行と専用スコープの寿命を管理する |


Builder は一度 Build すると再利用できません。子スコープで解決できない依存は親から解決されます。
Handler を破棄すると自身のスコープは無効になりますが、親スコープと外部から渡された
`IFlowContext` インスタンスは破棄しません。

### 基本的な手続き
```text
Program
  ├─ ScopeBuilder に Port 実装と Interaction を登録する
  ├─ ScopeBuilder から ScopeHandler を構築する
  ├─ SystemFlowBuilder に追加・上書きする Port 実装と Interaction を登録する
  ├─ SystemFlowBuilder から、ScopeHandler の依存関係を継承する SystemFlowHandler を構築する
  └─ SystemFlowHandler に IFlowContext を渡して実行する
```

### 実際のプログラム例

以下は、実際に単純な依存関係を構築して SystemFlow を実行する例や、
依存スコープに親子関係を持たせるコード例です。

<details>
<summary> 実際に SystemFlow を組み立てて実行するコードの例を表示 </summary>

```csharp
public static async Task RunSystem(IFlowContext context)
{
  // -------------------------------------------------
  // # case 1. 単純な SystemFlow 依存スコープの構築と実行
  // -------------------------------------------------

  // 依存解決の登録
  var sharedScopeBuilder = new ScopeBuilder();
  sharedScopeBuilder.UseFunction<IMyFunctionA, MyFunctionA>() // Function の登録
      .UseFunction<IMyFunctionB, MyFunctionB>()
      .UseInteraction<MyInteractionA>() // Interaction の登録
      .Apply(ConsoleBuilder.Profile); // 事前定義した依存解決プロファイルの登録

  // ScopeHandler の構築
  using var sharedScopeHandler = sharedScopeBuilder.BuildScope();

  // SystemFlowHandler の構築
  using var flowHandlerA = sharedScopeHandler
      .BuildSystemFlow<MySystemFlowA, IFlowContext>();

  // 💡 糖衣構文を利用しない場合、「SystemFlowHandler の構築」を以下のように記述できます。
  // // SystemFlowBuilder を用いた SystemFlowHandler の構築
  // using var flowHandlerA = new SystemFlowBuilder<IFlowContext>()
  //    .BuildSystemFlow<MySystemFlowA>(sharedScopeHandler);

  // SystemFlowA の実行
　var endA = await flowHandlerA.ExecuteAsync(context);

  // 実行エラーの可視化
  if (endA.HasException) Console.WriteLine(endA.Exception!);

  // -------------------------------------------------
  // # case 2. 複雑な SystemFlow 依存スコープの構築と実行
  // -------------------------------------------------

  // 上書きする依存解決の登録
  var localScopeBuilder = new ScopeBuilder();
  localScopeBuilder
      .UseFunction<IMyFunctionA, MyFunctionA_Custom>();

  // sharedScopeHandler の依存関係を継承した ScopeHandler の構築
  using var localScopeHandler = localScopeBuilder.BuildScope(sharedScopeHandler);

  // SystemFlow 独自の、誰にも共有されない依存解決の登録
  var flowBuilderB =  new SystemFlowBuilder<IFlowContext>();
  flowBuilderB
      .UseFunction<IMyFunctionC, MyFunctionC>()
      .UseInteraction<MyInteractionB>();

  // localScopeHandler の依存関係を継承した SystemFlowHandler の構築
  using var flowHandlerB = flowBuilderB.BuildSystemFlow<MySystemFlowB>(localScopeHandler);

  // SystemFlowB の実行
　var endB = await flowHandlerB.ExecuteAsync(context);

  // 実行エラーの可視化
  if (endB.HasException) Console.WriteLine(endB.Exception!);
}
```

</details>

### 実行時の依存ノードツリー <a id="runtime-dependency-tree"> </a>

SystemFlow、Interaction、Function Port 実装は `IDependencyNode` として、実行時に解決された具体的な依存インスタンスを `Dependency` に保持します。
`SystemFlowHandler.Root` は、生成済み SystemFlow の実行時インスタンスを公開します。

```text
DoorSystemFlow
  └─ OperateDoor
       ├─ ConsoleExceptionHandling
       ├─ ConsoleCancellationHandling
       ├─ ConsoleDoorOperation
       └─ ConsoleDoorReaction
```

`DependencyTreeView.GetDependencyTreeText` は、この Root から実体を再帰的に表示します。
これは DI 登録一覧ではなく、実行するフローを根とした観察用の構造です。

#### Samples.HelloDoor での実際の出力

> - InteractionFlow.Samples.HelloDoor.SystemFlows.DoorSystemFlow
>   - InteractionFlow.Samples.HelloDoor.Interactions.OperateDoor
>     - InteractionFlow.Standard.Console.Externals.Reactions.ConsoleExceptionHandling
>     - InteractionFlow.Standard.Console.Externals.Reactions.ConsoleCancellationHandling
>     - InteractionFlow.Samples.HelloDoor.Externals.Operations.ConsoleDoorOperation
>     - InteractionFlow.Samples.HelloDoor.Externals.Reactions.ConsoleDoorReaction

#### 依存ノードツリーの目的

このツリーは単なる表示機能ではなく、以下のような要素をつなぐ、信頼可能な「実行構成の証跡」として機能します。

- リファクタリング
- レビュー
- テスト
- 環境差分の確認
- 障害解析

具体的には、以下のような恩恵が得られます。

- DI の設定ミスにより、意図しない永続化実装や外部接続先が選択されていないか確認できる
- 起動時や障害発生時のログに、実際に解決された実行構成を記録できる
- テストで、本番用の外部 API やファイル操作ではなく、想定したスタブが使われているか確認できる
- コードレビューで、変更前後の実行構成を比較し、意図しない依存の追加を確認できる
- 単純な処理が多数の外部実装を引き込むなど、責務の肥大化や過剰な依存を発見できる
- 障害発生時に、Operation、Storage、Reaction など、調査対象となる実体と責任範囲を絞り込める
- 設計文書で想定した依存構造と、実際に組み立てられた構造の差を確認できる
- 利用者独自の Interaction や Function 実装を含む構成でも、共通の形式で診断情報を取得できる

#### Analyzer による保証

上記の目的を達成するためには、ツリーの内容が信頼できることが前提となります。

よって、このライブラリでは、Analyzer を用いてツリーの内容を保証します。
これにより、上記の目的を支援するとともに、以下のような副次的な恩恵が得られます。

- リファクタリングで追加した依存を `Dependency` へ含め忘れた場合、Analyzer で検出できる
- 派生クラスが追加した依存を基底クラスへ渡し忘れた場合、Analyzer で検出できる

詳細は、[保証と制約と責務](./LIBRARY_IMPLEMENTATION.md#guarantees) を参照してください。

## 値と処理結果の意味論 (Result, ReactionEnd, FlowEndToken, Entry) <a id="entry-result"></a>

このライブラリでは、**値の意味** が様々な場面で重要な役割を持ちます。

たとえば、System Flow や Interaction では、`IFlowContext` の文脈値を参照して実行経路を決定します。
ここでの文脈値は、**相互作用の中での現在の意味** を表現しています。

一方、`Storage` は、そのような文脈から独立して値を保持します。
つまり、**現在の意味から値を切り離し、別の意味を持たせる** ことで独立した情報として管理します。

また、Context Loop の実行経路を進める各要素は、次の実行経路を選択するために、
**処理の結果などを意味として解釈できる必要** があります。

そのために、このライブラリでは以下の要素をデフォルトの Entity として定義、利用しています。
| 型名 | 目的 |
| --- | --- |
| `Result` | 値を持たない結果に意味を持たせる |
| `Result<TValue>` | 値を持つ結果に意味を持たせる |
| `ReactionEnd` | `Reaction` の結果に意味を持たせる |
| `FlowEndToken` | `Interaction`/`SystemFlow` の結果に意味を持たせる |
| `Entry<TValue>` | 保持する値に意味を持たせる |

### Result

ソースコード: [`Result.cs`](../InteractionFlow.Core/Entities/Result.cs)

`Result` / `Result<TValue>` は、Function や補助処理における局所的な成功、成功値、失敗を表します。

これらは、意味を持つ結果として、主に以下のような特徴を持ちます。

- `TValue` は `Result<TValue>` に、`Exception` は `Result` / `Result<TValue>` にそれぞれ暗黙的に変換できる

  ```csharp
  Result Under(int num, int limit)
  {
    if (num >= limit) return new ArgumentOutOfRangeException($"num >= {limit}");
    else return Result.Success;
  }

  Result<int> ModIfNonNegative(int num, int mod)
  {
    if (mod == 0) return new DivideByZeroException("mod == 0");
    else if (num < 0) return new ArgumentOutOfRangeException($"num < 0");
    else return num % mod;
  }

  Result<int> Mod(int num, int mod)
  {
    if (mod == 0) return new DivideByZeroException("mod == 0");
    else return ModIfNonNegative(mod + num % mod, mod);
  }
  ```

- `Result` / `Result<TValue>` は、`null` 安全に例外や値を取り出せる

  ```csharp
  bool Result.Try([MaybeNullWhen(true)] out ResultException e)
  bool Result<TValue>.Try([MaybeNullWhen(false)] out TValue value,
    [MaybeNullWhen(true)] out ResultException e)
  ```

- フローを快適に記述・解読するために、拡張メソッドによって 同期・非同期の メソッドチェーン / Fluent API などが提供される

  [`ResultExtensions.cs`](../InteractionFlow.Core/Entities/ResultExtensions.cs),
  [`ResultTExtensions.cs`](../InteractionFlow.Core/Entities/ResultTExtensions.cs),
  [`ResultAsyncExtensions.cs`](../InteractionFlow.Core/Entities/ResultAsyncExtensions.cs),
  [`ResultTAsyncExtensions.cs`](../InteractionFlow.Core/Entities/ResultTAsyncExtensions.cs)
  ```csharp
  Result<int> ModUnder(int num, int mod, int limit)
  {
    // 最終的な戻り値は、Under が成功したときの modValue を持つ Result<int>。
    return ModIfNonNegative(num, mod)
      // ModIfNonNegative の実行が失敗なら、Mod を実行。
      .ThenError(error => Mod(num, mod))
      // 最後の実行が成功なら Under を実行、成功なら modValue を返す。
      .Then(modValue => Under(modValue, limit).Then(() => modValue.AsResult()))
      // 最後の実行が失敗なら、ModUnder の失敗として包んで伝播する。
      .ThenError(error =>
        new InvalidOperationException($"ModUnder error : {error.Message}", error));
  }
  ```
  ※ この例では ModUnder 自体の入力契約は省略しています。

- `Exception` から変換された失敗は、原則として `ResultException` でラップされ、元の例外は `InnerException` として保持される

  `ResultException` は DEBUG ビルドでのみ値を持つ `StackTrace? ResultCreationStackTrace { get; }` プロパティを持ち、この例外が生成された場所をトレースする

- `default(Result)` は例外を持たないため成功として扱われる

- `default(Result<TValue>)` は成功値を持たないため失敗として扱われる

- 意味の重複を避けるために、`Exception` 派生型の値（`Result<Exception>` など）は使用できない
- `Result<TValue>` の成功値に `null` は使用できない

### ReactionEnd

ソースコード: [`ReactionEnd.cs`](../InteractionFlow.Core/Entities/Contexts/ReactionEnd.cs)

`ReactionEnd` は、`Reaction` が User への反応を終えた時点で確定した成功、未解決例外、キャンセルを表します。

これは `Result` と同様に結果の意味を表しますが、
特に `Reaction` の結果に User への反応としての意味を持たせるために、以下のような特徴を持ちます。

- ライブラリの利用者にとって、`ReactionEnd` は、`Reaction` / `IReactionPort` の `protected` 関数によってのみ生成される

  ソースコード: [`IReactionPort.cs`](../InteractionFlow.Core/ExternalPorts/ReactionPorts/IReactionPort.cs),
  [`Reaction.cs`](../InteractionFlow.Core/Externals/Reactions/Reaction.cs)

  ```csharp
    // ReactionEnd
    internal static ReactionEnd Success { get; } = new(null);
    internal ReactionEnd(Exception? exception) { Exception = exception; }
  ```

  ```csharp
    // IReactionPort
    protected static ReactionEnd GetEnd(Exception? exception = null) =>
      exception == null ? ReactionEnd.Success : new(exception);
  ```

  ```csharp
    // Reaction : IReactionPort
    protected static ReactionEnd GetEnd(Exception? exception = null) =>
      IReactionPort.GetEnd(exception);
  ```

- `Reaction` / `IReactionPort` の派生クラスは、`GetEnd` 関数を用いて反応結果に意味を持たせる

  ```csharp
  // `Reaction` 派生型内のコード例
  ReactionEnd Write(string message)
  {
    //  User への反応に失敗した終了結果。例外で詳細な意味を表す。
    if (string.IsNullOrEmpty(message))
      return GetEnd(new ArgumentException("message is empty."));

    // User への反応。
    Console.WriteLine(message);

    // User への反応に成功した終了結果。
    return GetEnd();
  }
  ```

- 例外の種類から、成功、未解決例外、キャンセルを判定できる

  ```csharp
  // `ReactionEnd` 判定のコード例
  void HandleReactionEnd(ReactionEnd end)
  {
    if (end.HasCanceled) // end.Exception is not null and OperationCanceledException
    {
      Console.WriteLine("Reaction Canceled");
    }
    else if (end.HasException) // end.Exception is not null
    {
      Console.WriteLine($"Reaction Error : {end.Exception!.Message}");
    }
    else
    {
      Console.WriteLine("Reaction succeeded.");
    }
  }
  ```

- `ReactionEnd` の成功は、`Reaction` が反応を実行したことを表すが、ライブラリとしてそれが保証されるものではない
- `ReactionEnd` の成功は、User が実際にそれを観測したことまでは保証しない

### FlowEndToken

ソースコード: [`FlowEndToken.cs`](../InteractionFlow.Core/Entities/Contexts/FlowEndToken.cs)

`FlowEndToken` は、`Reaction` が確定した `ReactionEnd` と、
`Interaction` または `SystemFlow` の実行境界に渡された `IFlowContext` インスタンスを結び付けます。

これも `Result` や `ReactionEnd` と同様に結果の意味を表しますが、
特に実行境界を隔てた終了結果に、User との関係や相互作用としての意味を持たせるために、以下のような特徴を持ちます。

- 終了結果と実行境界を、以下のプロパティで保持する

  ```csharp
  ReactionEnd FlowEndToken.End { get; }
  IFlowContext FlowEndToken.LastContext { get; }
  ```

- ライブラリの利用者にとって、`FlowEndToken` は、`IInteraction` / `Interaction` / `ISystemFlow` / `SystemFlow` の `protected` 関数によってのみ生成される

  ソースコード: [`IInteraction.cs`](../InteractionFlow.Core/Interactions/IInteraction.cs),
  [`Interaction.cs`](../InteractionFlow.Core/Interactions/Interaction.cs),
  [`ISystemFlow.cs`](../InteractionFlow.Core/SystemFlows/ISystemFlow.cs),
  [`SystemFlow.cs`](../InteractionFlow.Core/SystemFlows/SystemFlow.cs)

  ```csharp
    // FlowEndToken
    internal FlowEndToken(IFlowContext lastContext, ReactionEnd end)
    {
        LastContext = lastContext;
        End = end;
    }
  ```

  ```csharp
    // IInteraction
    protected static FlowEndToken GetEnd(IFlowContext context,
      ReactionEnd reactionEnd) => new(context, reactionEnd);

    // Interaction : IInteraction
    protected static FlowEndToken GetEnd(IFlowContext context,
      ReactionEnd reactionEnd) => IInteraction.GetEnd(context, reactionEnd);
  ```

  ```csharp
    // ISystemFlow
    protected static FlowEndToken GetEnd(IFlowContext context,
      FlowEndToken interactionEnd) => new(context, interactionEnd.End);

    // SystemFlow : ISystemFlow
    protected static FlowEndToken GetEnd(IFlowContext context,
      FlowEndToken interactionEnd) => ISystemFlow.GetEnd(context, interactionEnd);
  ```

- `IInteraction` / `ISystemFlow` の派生クラスは、`GetEnd` 関数を用いて終了結果に意味を持たせる

  `IInteraction` 派生型内のコード例
  ```csharp
  async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
  {
    ReactionEnd? re = null;

    try
    {
      // 相互作用を10回実行
      for(int i = 0; i < 10; i++)
      {
        var op = await operation.Operate();
        re = await reaction.React(op);
      }

      // 最後の reaction の結果を、 context と結び付けて終了結果とし、相互作用の境界を閉じる
      return IInteraction.GetEnd(context, re!);
    }
    catch(Exception e)
    {
      // 想定外のエラーをハンドリングして反応に変換し、相互作用の境界を保つ
      re = await errorReaction.React(e);
      return IInteraction.GetEnd(context, re);
    }
  }
  ```

  `ISystemFlow<in TContext>` 派生型内のコード例
  ```csharp
  async Task<FlowEndToken> ExecuteAsync(TContext context)
  {
    var end = await interactionA.ExecuteAsync(context);

    // 失敗した interactionA の結果を context に結びなおして終了結果とし、User との関係の境界を閉じる
    // キャンセルの場合は引き続きフローを実行する
    if (end.HasException && !end.HasCanceled)
      return ISystemFlow.GetEnd(context, end);

    end = await interactionB.ExecuteAsync(context);

    // interactionB の結果を context に結びなおして終了結果とし、User との関係の境界を閉じる
    return ISystemFlow.GetEnd(context, end);
  }
  ```

- `ISystemFlow<TContext>` の既定実装である `SystemFlow<TContext>` は、渡された `context` を戻り値の `FlowEndToken` に結びつけることを保証する
- `IInteraction` の既定実装である `Interaction` は、渡された `context` を戻り値の `FlowEndToken` に結びつけることを保証する
- `IInteraction` の既定実装である `Interaction` は、実行中に起きた例外をキャッチして `IExceptionPort` や `ICancellationPort` で反応することを保証する
- `FlowEndToken` は `LastContext` の所有権を取得せず、破棄もしない

### Entry

ソースコード:
[`Entry.cs`](../InteractionFlow.Core/Entities/Entry.cs),
[`RefEntry.cs`](../InteractionFlow.Core/Entities/RefEntry.cs)

`Entry<TValue>` は、保持する値に、型としての解決や更新可能性などの意味を持たせるための基底クラスです。

これは `Result` / `ReactionEnd` / `FlowEndToken` と同様に値の意味を表しますが、
特に `IFlowContext` や Storage が値固有の知識を持たずに、
値を保持・保存できるようにするために、以下のような特徴を持ちます。

- `Entry<TValue>` は値を保持し、`Parse<T>` 関数によって要求された型として解決する

  ```csharp
  // Entry<TValue>
  TValue? Entry<TValue>.Value { get; protected set; }
  Result<T> Entry<TValue>.Parse<T>()
  ```

  ```csharp
  // 値を取得するコード例
  Result<int> ParseInt()
  {
    Entry<int> entry = new RefEntry<int>(10);
    return entry.Parse<int>();
  }
  ```

- Entry が別の Entry を保持する場合は、要求された型の値まで再帰的に解決する

  ```csharp
  // ネストされた値を解決するコード例
  Result<int> ParseNestedEntry()
  {
    var inner = new RefEntry<int>(10);
    var outer = new RefEntry<Entry<int>>(inner);

    // outer -> inner -> 10 の順に解決する。
    return outer.Parse<int>();
  }
  ```

- `RefEntry<TValue>` は、`Value` の setter によって同じ Entry が表す値を差し替える

  ```csharp
  // RefEntry<TValue> : Entry<TValue>
  TValue? RefEntry<TValue>.Value { get; set; }
  ```

  ```csharp
  // 値を差し替えるコード例
  Result<int> Update<T>(Entry<T> entry)
  {
    // Entry から RefEntry<int> として解決。
    return entry.Parse<RefEntry<int>>()
      .Then(reference =>
      {
        // 値を差し替える。
        reference.Value = 123;
        // int として再取得。
        return entry.Parse<int>();
      });
  }
  ```

- `PersistentEntry<TPersistenceId, TValue>` は、値と永続化先 ID の関係に意味を持たせる

  ソースコード:
  [`PersistentEntry.cs`](../InteractionFlow.Core/ExternalPorts/StoragePorts/Entries/PersistentEntry.cs),
  [`PersistentEntryExtensions.cs`](../InteractionFlow.Core/ExternalPorts/StoragePorts/Entries/PersistentEntryExtensions.cs)

  ```csharp
  // PersistentEntry<TPersistenceId, TValue> : Entry<TValue>
  TPersistenceId PersistentEntry<TPersistenceId, TValue>
    .PersistenceId { get; }
  ```

  ```csharp
  // 値の永続化を扱うコード例
  async Task<Result<string>> SaveAndLoad(
    IPersistencePort<string, string> persistence)
  {
    var entry = new PersistentEntry<string, string>("id", "value");

    // Entry は値と保存先 ID の関係を担う。
    // Persistence Port は保存方法を担う。
    return await entry.Save(persistence)
      .ThenAsync(() => entry.Load(persistence));
  }
  ```

- 値が `null`、または要求された型として解決できない場合は、`EntryValueNotFoundException` を持つ失敗 `Result<T>` を返す

- Entry の参照に循環がある場合は、`InvalidOperationException` を持つ失敗 `Result<T>` を返す

- `PersistentEntry` の `Save`、`Load`、`Exists`、`DeleteAndReset` は、Persistence Port の処理結果を `Result` / `Result<TValue>` として伝播する

- `Entry<TValue>` を破棄すると、保持値が `IDisposable` の場合はその値を破棄し、`Value` を `default` にする

`PersistentEntry<TPersistenceId, TValue>` については、[Storage と永続化](#storage-persistence) でも詳しく扱います。

## 文脈の構成 (Context) <a id="context"></a>

概念上の `Context` は Context Loop のある時点における User と System の現在の共有文脈です。

一方、実装上の `IFlowContext` とその派生によるインスタンスは  `Context` の実装上の部分投影であり、
`SystemFlow` や `Interaction`、`Function` などが扱う文脈値を API 呼び出しの定義から分離するために利用されます。

概念上の `Context` に対応する実装上の要素は、`IFlowContext` 以外に関数の戻り値や引数のオブジェクト等を含む場合があります。

### `IFlowContext` の契約

ソースコード: [`IFlowContext.cs`](../InteractionFlow.Core/Entities/Contexts/IFlowContext.cs)

`IFlowContext` は、そのインスタンスに紐づくキャンセル制御と、型を指定した文脈値の取得を提供します。

```csharp
public interface IFlowContext
{
    CancellationObject Cancellation { get; }

    bool TryGet<T>(out T value);
}
```

利用側は、特定の実装へキャストしなくとも、原則として `TryGet<T>` により必要な文脈値を要求できます。
`TryGet<T>` は値がない場合に `false` を返しますが、実装上のイレギュラーには例外を発生させる場合があります。
例えば `ScopedFlowContext` が Entry の循環参照を検出した場合や、破棄後に呼ばれた場合は例外になります。

### `FlowContext`

ソースコード: [`FlowContext.cs`](../InteractionFlow.Core/Entities/Contexts/FlowContext.cs)

`FlowContext`は、`IFlowContext` の最小実装、および拡張実装のためのベースクラスの一つです。

最小限の実装として、`TryGet<T>` から `CancellationObject` を取得するための実装を提供しています。

```csharp
// FlowContext : IFlowContext
public virtual bool TryGet<T>([MaybeNullWhen(false)] out T value)
{
  if (Cancellation is T _value)
  {
    value = _value;
    return true;
  }
  else
  {
    value = default;
    return false;
  }
}
```

### `ScopedFlowContext`

ソースコード: [`ScopedFlowContext.cs`](../InteractionFlow.Core/Entities/Contexts/ScopedFlowContext.cs)

`ScopedFlowContext`は、任意の `IFlowContext` を親として、追加の文脈値を重ねるためのクラスです。
追加された文脈値は、`TryGet<T>` において親の同型の文脈値よりも優先されます。

内部では `Entry` のリストとして追加の文脈値を保持、提供しています。
`TryGet<T>` の実装では `Entry.Parse<T>()` を利用しているため、Entry と同等の再帰的解決能力を有します。
また、`TryGet<T>` 中に Entry の値の未発見以外の例外を検出した場合や、`Dispose()` 後に `TryGet<T>` が呼ばれた場合は例外をスローします。

```csharp
// ScopedFlowContext : IFlowContext

private List<IEntry> Values => values ??
  throw new ObjectDisposedException(nameof(ScopedFlowContext));

public bool TryGet<T>([MaybeNullWhen(false)] out T value)
{
  // Dispose() 後は Values プロパティが例外をスローする
  for (int i = Values.Count - 1; i >= 0; i--)
  {
    var item = Values[i];

    if (item.Parse<T>().Try(out var v, out var e))
    {
      value = v;
      return true;
    }
    else if (e.InnerException is null or not EntryValueNotFoundException)
    {
      throw e;
    }
  }
  return parentContext.TryGet(out value);
}
```

文脈値の追加には `With<T>(T value)` 関数を利用します。

```csharp
// ScopedFlowContext : IFlowContext
public ScopedFlowContext With<T>(T value)
{
  if (value is null)
  {
    throw new ArgumentNullException(nameof(value));
  }

  // Box<T> は、内部で定義される専用の Entry
  Values.Add(new Box<T>(value));

  return this;
}
```

<details>

<summary> ScopedFlowContext で一時的な文脈値を利用するコード例 </summary>

```csharp
public void RunWithCustomContext(IFlowContext context)
{
  RunWithContext(context);
  // Console : "[UNKNOWN NUM] UNKNOWN TEXT" (or Nothing)

  using var customContext = new ScopedFlowContext(context)
    .With(new MyText("I'm Additional Context"))
    .With(new RefEntry<MyNumber>(new MyNumber(0)));

  RunWithContext(customContext);
  // Console : "[0] I'm Additional Context"
  RunWithContext(customContext);
  // Console : "[1] I'm Additional Context"
}

public void RunWithContext(IFlowContext context)
{
  if (context.TryGet<MyNumber>(out MyNumber num) &&
    context.TryGet<MyText>(out MyText text))
  {
    Console.WriteLine($"[{num.Value}] {text.Value}");
  }

  if (context.TryGet<RefEntry<MyNumber>>(out RefEntry<MyNumber> refNum))
  {
    refNum.Value = new MyNumber(num.Value + 1);
  }
}
```

なおこの時、`using var customContext` のブロックが終了して `customContext` が破棄されても、`With`で追加した文脈は破棄されません。
追加した文脈値を破棄する責務は、API 利用者の側にあります。

</details>

### `CancellationObject`

ソースコード: [`CancellationObject.cs`](../InteractionFlow.Core/Entities/Contexts/CancellationObject.cs)

`IFlowContext.Cancellation { get; }` は `CancellationObject` のインスタンスを保持・提供します。

`CancellationObject` は、キャンセルトークンソース（`private CancellationTokenSource? tokenSource`）と、キャンセル時に完了を待つタスク（`private readonly List<Task> currentTasks`）を持ち、キャンセル処理の管理を自身のインスタンスによって単位化します。

主要な関数は以下の通りです。

| 関数 | 機能の要約 |
| --- | --- |
| `CancellationToken GetToken()` | タスク実行側として、キャンセル要求を処理するために、トークンを取得する。キャンセル処理のタイミングや内容は呼び出し側が実装する。|
| `void AddCancelableTask(Task task)` | タスク実行側として、タスク待機側に待機してほしいタスクを登録する。|
| `ValueTask<Result> WaitAndResetAsync()` | タスク待機側として、タスク実行側が待機してほしいタスクを待機する。|
| `void Cancel()` | タスク停止要求側で、キャンセルトークンを通じてタスク実行側にキャンセルを要求する。タスクそのものを強制終了するのではなく、要求のみを伝える。|

> 1. タスク停止要求側は、キャンセル要求のみを伝えます。
> 2. タスク実行側は、待機してほしいタスクを登録しつつ、キャンセル要求を監視・処理します。
> 3. タスク待機側は、登録されたタスクを待機します。

これにより、安全なキャンセルの要求・処理・待機を実装できます。
また、`CancellationObject` の状態は必要なタイミングで初期化され、何度でも安全に使いまわすことができます。

<details>

<summary> CancellationObject の実装コードの要約 </summary>

```csharp
// CancellationObject
public class CancellationObject
{
  private CancellationTokenSource? tokenSource;
  private readonly List<Task> currentTasks = [];

  ...

  // トークンの取得
  public CancellationToken GetToken()
  {
      lock (lockObject)
      {
          tokenSource ??= new();
          return tokenSource.Token;
      }
  }

  // 待機タスクの追加
  public void AddCancelableTask(Task task)
  {
    lock (lockObject)
    {
      ...

      currentTasks.Add(task);
    }
  }

  // キャンセルリクエスト
  public void Cancel()
  {
    CancellationTokenSource source;

    lock (lockObject)
    {
      tokenSource ??= new();
      source = tokenSource;
    }

    if (!source.IsCancellationRequested)
      source.Cancel();
  }

  // キャンセル待機
  public ValueTask<Result> WaitAndResetAsync()
  {
    CancellationTokenSource source;
    Task[] tasks;
    bool isCompleted;

    lock (lockObject)
    {
      // キャンセルがリクエストされていなければ即時失敗で終了
      if (tokenSource == null || !tokenSource.IsCancellationRequested)
        return new(new InvalidOperationException("Cancellation is not requested."));

      // source / tasks / isCompleted で状態をスナップショット化
      source = tokenSource;
      tasks = [.. currentTasks];
      tokenSource = null;
      currentTasks.Clear();
      isCompleted = tasks.Length == 0 || tasks.All(e => e.IsCompleted);
    }

    if (isCompleted)
    {
      // 全てのタスクが終了していれば即時終了
      // 例外があれば AggregateException で集約し、Result として返す。
      source.Dispose();
      return new(GetCompletedResult(tasks));
    }

    // すべてのタスクを待機する。
    // キャンセルは無視、それ以外の例外はすべて集約して Result として返す。
    // 最後に CancellationTokenSource を必ず破棄する。
    return new(WaitAllAsync(source, tasks));
  }

  ...
}
```

</details>

#### `Interaction` での利用例

主な利用例は `Interaction` 基底実装における `OnCancellation` の自動実行です。
この処理は以下のような時系列で実行されます。


| 時系列 | その他制御 | `Interaction` の内部制御 |
| :---: | :--- | :--- |
| T1 | **`SystemFlow`**: `Interaction.ExecuteAsync()` を実行 | **`ExecuteAsync()`**: `ExecuteCoreAsync()` を実行、 `CancelableTask` を作成・実行して `CancellationObject.AddCancelableTask(CancelableTask)` で登録、`ExecuteCoreAsync()` を待機 <br/><br/> **`CancelableTask`**: `ExecuteCoreAsync()` を待機 <br/><br/>**`ExecuteCoreAsync()`**: Interaction としてのメイン処理を実行、`CancellationObject.GetToken()` で `CancellationToken` を取得・監視 |
| T2 | **`外部トリガー`**: `IFlowContext.Cancellation` を通じて `Cancel()` を呼び、キャンセルを通知（待機はしない）<br/><br/> **`HandleCancellationAsync()`**: `IFlowContext.Cancellation` を通じて `await WaitAndResetAsync()` でキャンセルを待機 |  **`ExecuteCoreAsync()`**: キャンセルの通知に応じて安全に処理をキャンセル扱いで終了 <br/><br/>**`CancelableTask`**: `ExecuteCoreAsync()` がキャンセル扱いで終了したら、`OnCancellation()` を実行・待機 <br/><br/>**`ExecuteAsync()`**: `ExecuteCoreAsync()` がキャンセル扱いで終了したら、`HandleCancellationAsync()` を実行・待機 |
| T3 | **`SystemFlow`**: `Interaction.ExecuteAsync()` の待機を完了 <br/><br/>**`HandleCancellationAsync()`**: `CancellationObject.WaitAndResetAsync()` を通じて `Interaction.OnCancellation()` の待機が完了、キャンセル後のハンドリングを実行・完了 | **`OnCancellation()`**: Interaction としてのキャンセル処理を実行・完了<br/><br/> **`CancelableTask`**: `OnCancellation()` の完了に伴って終了 <br/><br/> **`ExecuteAsync()`**: `HandleCancellationAsync()` の完了に伴って終了 |

#### この設計の利点

この設計では、`CancellationObject` が **キャンセルのライフサイクル全体** を管理します。

`CancellationObject` は、キャンセル要求の通知、実行中タスクの管理、キャンセル完了の待機、そして内部状態のリセットまでを一貫して担当します。

まず、`CancellationObject.Cancel()` はキャンセルを通知するだけで、処理の完了を待機しません。
そのため、UI や上位の `SystemFlow` は、キャンセル処理の終了を待つことなく次の制御へ進めます。

一方、キャンセル完了まで待機する必要がある場合は、`CancellationObject.WaitAndResetAsync()` を利用します。
このメソッドは、呼び出し時点で登録されている全ての待機タスクが **完了**・**キャンセル停止**・**例外停止** のいずれかの状態になるまで、待機を続けます。

特に「`Interaction` での利用例」では、`CancelableTask` を `CancellationObject` の管理対象として登録しています。
この `CancelableTask` は `ExecuteCoreAsync()` の終了だけでなく、`OnCancellation()` による終了処理まで完了したことを保証しているため、`WaitAndResetAsync()` も同様にこの完了を保証します。

このように、キャンセルの通知と完了待機を分離することで、通知する側は `Cancel()` を呼び出すだけでよく、待機する側は `WaitAndResetAsync()` のみを待機すればよくなります。
また、`Interaction` は終了処理を `OnCancellation()` に分離でき、`SystemFlow` は `ExecuteAsync()` を一つの待機対象として扱えます。

さらに、`CancellationObject` を `IFlowContext` から提供することで、実行構造に応じて、関連する処理には同じ停止要求を共有できます。
これにより、キャンセルの伝播を保ちながら、処理の終了待機を安全に管理できます。

## 機能の分離 (Functions) <a id="functions"></a>

### Function における意味論

Interaction Flow Architecture では、User と System の境界で行われる入出力や外部環境との連携を Function として分離します。

この Function はライブラリの実装において、機能の意味の定義である Function Port と、その実装である Function External に分けられます。

| アーキテクチャモデル | 実装モデル | 実装上の役割 |
| :---: | :---: | :--- |
| Interaction が実行する機能 | Function Port | 機能の意味の定義 |
| Function を実現する実行環境 | Function External | 機能の実装 |

この分離によって「依存性逆転の原則」を、機能の意味の境界で実現します。
Function の呼び出し元である Interaction は、Function Port を用いて、機能の意味に基づいた相互作用を実装し、
Function External は、特定の環境（CUI、GUI、その他外部ライブラリ、外部ライブラリに依存しない独自実装、テスト用ダミーなど）を利用して、機能の意味を実装します。

Port と External の境界は `ExternalPorts` / `Externals` の namespace と Analyzer の依存規則で表します。
現在の実装では、Operation、Reaction、Storage、SilentExternal の基底実装は、いずれも `IFlowNode.Layer` として
`FunctionPort` を返すため、実行時メタデータだけではこの境界を区別できません。

さらに、Function Port が表現する機能は、以下の四つの種類に分類されます。

| アーキテクチャモデル | 実装モデル | 実装上の役割 |
| :---: | :---: | :--- |
| Operation | `IOperationPort` とその派生 | User による操作を仮想化する |
| Reaction | `IReactionPort` とその派生 | User が観測できる反応を仮想化する |
| Storage | `IStoragePort` とその派生 | Context の文脈とは独立して、System 内で記録する値の管理を仮想化する |
| Silent External | `ISilentExternalPort` とその派生 | User との相互作用や System 内での記録を直接の目的としない機能を仮想化する |

この分類は、**User による操作** / **System による反応** / **System が記録する値の管理** / **System 内のその他の機能** という機能としての意味に基づいた分類です。
この分類により、各機能の責務の境界は、機能の意味の境界としてより明確になります。

### Function の状態

ソースコード: [`IFlowNodeStateful.cs`](../InteractionFlow.Core/Entities/Architectures/IFlowNodeStateful.cs)、[`FunctionStateScope.cs`](../InteractionFlow.Core/Entities/Architectures/FunctionStateScope.cs)

Function Port は `IFlowNodeStateful` を継承します。
これは、全ての Function が状態を持てることを意味します。

| 種類 | Operation | Reaction | Storage | Silent External |
| :---: | --- | --- | --- | --- |
| **状態** | 入力の加工バッファ、設定など | 出力の加工バッファ、設定など | 保持データ、操作履歴、設定など | キャッシュ、設定など |

これらの状態は、文脈値を提供する `IFlowContext` とは以下のような意味の違いによって区別されます。

| ライブラリの実装 | 意味 |
| --- | --- |
| `IFlowContext` の状態 | `System Flow` および `Interaction` の中で、経路の決定のために参照したり、次に引き継いだりするための文脈値。Function の動作にも影響する場合がある。 |
| 各 Function の状態 | 各機能が、機能としての動作を実現するために保持している状態、および動作の詳細を決定するプロパティ。動作結果を通して間接的に `Interaction` の経路に影響する場合はあるが、通常は `Interaction` から直接参照されない。 |

`IFlowNodeStateful.ForceResetMemoryState()` は、この Function の状態を強制的に初期化する関数です。
初期化の内容は派生先が実装します。
この関数を用いてバッファやキャッシュなどを初期化することで、テストや使いまわしにおける特定のトラブルを避けることができます。
ただし、特に `Storage` 系などでは、通常のライフサイクルから外れた動作になる可能性があるため、強制的な初期化は慎重に実行する必要があります。

`IHasFunctionState<TState>` を継承・実装する Function は、 `FunctionStateScope<TState>` を用いることができます。
`FunctionStateScope<TState>` は、`TState` で表現される状態を一時的に差し替え、破棄時に元へ戻すスコープです。
完全な初期化である `ForceResetMemoryState()` とは異なり、スコープ生成時点での状態まで戻します。

`IFlowNodeStateful` は全ての Function が必ず継承し、その `ForceResetMemoryState()` 関数は確実に全ての状態を初期化する必要があります。
一方で、`IHasFunctionState<TState>` の継承は義務ではなく、また継承時も `TState` に全ての状態を集約する必要はありません。

<details> <summary> State を持った単純な Reaction のコード例 </summary>

```csharp
namespace MyApp.Entities
{
    public class WriterOption : IFunctionState<WriterOption>
    {
        public ConsoleColor Color { get; set; } = ConsoleColor.Gray;

        // IFunctionState<WriterOption>.Copy()
        public WriterOption Copy()
        {
            return new() { Color = Color };
        }
    }
}

namespace MyApp.ExternalPorts
{
    public interface ITextWriter : IReactionPort, IHasFunctionState<WriterOption>
    {
        public void Write(string text);
    }
}

namespace MyApp.Externals
{
    public sealed class ConsoleTextWriter : Reaction, ITextWriter
    {
        // IHasFunctionState<WriterOption>.State
        public WriterOption State { get; set; } = new WriterOption();

        public override void ForceResetMemoryState()
        {
            State = new WriterOption();
        }

        public void Write(string text)
        {
            Console.ForegroundColor = State.Color;
            Console.Write(text);
        }
    }
}

namespace MyApp.Interactions
{
    public sealed class FunctionStateSample
    {
        public static void WriteWithRed(ITextWriter writer)
        {
            writer.Write("Default Color Text");

            // scope は、この時点での State をコピーして保持する
            using FunctionStateScope<WriterOption> scope = writer.GetStateScope();

            scope.State.Color = ConsoleColor.Red;
            writer.Write("Red Text");
            // スコープを抜けた時に、コピーしていた State を set することで状態を復元する
        }
    }
}
```
</details>

### Operation

ソースコード: [`IOperationPort.cs`](../InteractionFlow.Core/ExternalPorts/OperationPorts/IOperationPort.cs)、[`Operation.cs`](../InteractionFlow.Core/Externals/Operations/Operation.cs)

Operation は、User の入力・操作を Interaction が扱える値へ変換します。
入力結果は戻り値だけではなく、`IFlowContext` に蓄積する事も出来ます。
また、実際に User を介した入力だけではなく、仮想的な入力を実装する事も出来ます。

`IFlowContext` への結果の蓄積については、[ライブラリの実装 - Context 更新の原則](./LIBRARY_IMPLEMENTATION.md#context-update-principle) も参照してください。

以下は Operation の最小コード例です。
```csharp
namespace MyApp.ExternalPorts
{
    public interface ITextOperation : IOperationPort
    {
        ValueTask<string> ReadAsync(IFlowContext context);
    }
}

namespace MyApp.Externals
{
    public sealed class ConsoleTextOperation : Operation, ITextOperation
    {
        public override void ForceResetMemoryState() { }

        public ValueTask<string> ReadAsync(IFlowContext context)
            => new(Console.ReadLine() ?? string.Empty);
    }
}

namespace MyApp.Externals
{
    public sealed class DummyTextOperation : Operation, ITextOperation
    {
        // Operation は状態を持てる
        public string DummyText { get; set; } = "I'm Dummy Text.";

        public override void ForceResetMemoryState() { DummyText = "I'm Dummy Text."; }

        public ValueTask<string> ReadAsync(IFlowContext context)
        {
            // 入力結果を IFlowContext に蓄積する例
            if (context.TryGet<DummyTextFlag>(out var dummyTextFlag))
                dummyTextFlag.Value = true;

            return new(DummyText);
        }
    }
}
```

### Reaction

ソースコード: [`IReactionPort.cs`](../InteractionFlow.Core/ExternalPorts/ReactionPorts/IReactionPort.cs)、[`Reaction.cs`](../InteractionFlow.Core/Externals/Reactions/Reaction.cs)

Reaction は、Interaction の過程や終端における System 側の相互作用として、必要に応じて `IFlowContext` を更新し、その結果を User が観測できる反応を返します。

`IFlowContext` への影響の適用については、[ライブラリの実装 - Context 更新の原則](./LIBRARY_IMPLEMENTATION.md#context-update-principle) も参照してください。

以下は Reaction の最小コード例です。
```csharp
namespace MyApp.ExternalPorts
{
    public interface ITextReaction : IReactionPort
    {
        ValueTask<ReactionEnd> ReactAsync(IFlowContext context, string text);
    }
}

namespace MyApp.Externals
{
    public sealed class ConsoleTextReaction : Reaction, ITextReaction
    {
        // Reaction は状態を持てる
        private int line = 0;

        public override void ForceResetMemoryState() { line = 0; }

        public ValueTask<ReactionEnd> ReactAsync(IFlowContext context, string text)
        {
            if (!context.TryGet<ExitFlag>(out var exitFlag))
                return new(GetEnd(new Exception("Not found ExitFlag in context.")));

            if (text == "Exit")
            {
                exitFlag.Value = true;
                Console.WriteLine($"Goodbye.");
            }
            else
            {
                Console.WriteLine($"[{line}] {text}");
                line++;
            }

            return new(GetEnd());
        }
    }
}
```

### Storage

ソースコード: [`IStoragePort.cs`](../InteractionFlow.Core/ExternalPorts/StoragePorts/IStoragePort.cs)、[`Storage.cs`](../InteractionFlow.Core/Externals/Storages/Storage.cs)

Storage は、`Context` から独立して、キーに対応するメモリ上の値を所有します。

```csharp
namespace MyApp.Entities
{
    public class TextStorageItem
    {
        public string Text { get; set; } = "Default Text";
    }

    public record struct TextStorageKey(int Value);
}

namespace MyApp.ExternalPorts
{
    public interface ITextStorage : IStoragePort<TextStorageKey, TextStorageItem>
    {
    }
}

namespace MyApp.Externals
{
    public sealed class TextStorage : Storage<TextStorageKey, TextStorageItem>,
        ITextStorage
    {
        protected override Result<TextStorageItem> CreateNewValue(TextStorageKey key)
            => new TextStorageItem() { Text = $"Item {key}" };

        protected override Result CanRemoveValue(TextStorageKey key,
            TextStorageItem value)
            => Result.Success;
    }
}
```

また、`CreateNewValue` や `CanRemoveValue` の失敗を利用して、値の追加・削除の条件を実装する事も出来ます。

```csharp
namespace MyApp.Externals
{
    public sealed class TextStorage2 : Storage<TextStorageKey, TextStorageItem>,
    ITextStorage
    {
        // 非負の key の要素だけ追加可能
        protected override Result<TextStorageItem> CreateNewValue(TextStorageKey key)
            => key.Value < 0 ?
                new ArgumentOutOfRangeException($"key.Value = {key.Value} < 0") :
                new TextStorageItem() { Text = $"Item {key}" };

        // Text が空の要素だけ削除可能
        protected override Result CanRemoveValue(TextStorageKey key,
            TextStorageItem value)
            => string.IsNullOrEmpty(value.Text) ?
                Result.Success :
                new InvalidOperationException(
                    "string.IsNullOrEmpty(value.Text) == false");
    }
}
```

### Storage を用いた永続化 <a id="storage-persistence"></a>

ソースコード: [`Storage.cs`](../InteractionFlow.Core/Externals/Storages/Storage.cs)、[`PersistentEntry.cs`](../InteractionFlow.Core/ExternalPorts/StoragePorts/Entries/PersistentEntry.cs)、[`IPersistencePort.cs`](../InteractionFlow.Core/ExternalPorts/StoragePorts/PersistencePorts/IPersistencePort.cs)、[`ISerializerPort.cs`](../InteractionFlow.Core/ExternalPorts/StoragePorts/SerializerPorts/ISerializerPort.cs)

Storage を用いた永続化では、**値の表現**、**値の所有**、**形式変換**、**実際のI/O** を分けて実現します。

| 要素 | 主な責務 | 代表的な API / 型 |
| --- | --- | --- |
| **値の表現** | メモリ上の値と永続化先 ID を関連付ける | `PersistentEntry<TKey, TValue>` |
| **値の所有** | メモリ上の値をキー単位で所有する | `IStoragePort`、<br/>`Storage<TKey, PersistentEntry<TKey, TValue>>` |
| **形式変換** | 値と保存のための中間表現を相互変換する | `ISerializerPort<TData, TValue>` |
| **実際のI/O** | 保存先との読み書きを行う | `IPersistencePort<TKey, TValue>` |

ここで用いられている型引数は、それぞれ以下の情報を表現します。

| 型引数名 | 表現する情報 | 代表的な API / 型 |
| --- | --- | --- |
| `TValue` | 永続化 ID とは切り離された、情報の本体 | 例における `TextStorageItem` や、その他利用者が定義した Entity など |
| `TKey` | 情報の本体とは切り離された、永続化 ID | 例における `TextStorageKey`や、その他利用者が定義した Entity など |
| `TData` | `TValue` を保存するための中間表現 | `Stream` や、`string` など |

ここで、Storage のキーと永続化 ID がどちらも `TKey` 型であることは必須ではありませんが、ベストプラクティスです。
`値の識別子` と `永続データの識別子` を分離すると、値との対応が複雑化し、予期せぬトラブルが発生する可能性が高くなります。
少なくとも、`値の識別子` と `永続データの識別子` は相互変換が可能であることが望ましく、また、相互変換が可能であるならば、表現の差を `IPersistencePort` で吸収し、識別子は統一することを検討することを推奨します。

この責務分離により、以下の分離が達成されます。
- **PersistentEntry**
  - **値の所有**、**形式変換**、**実際のI/O** の詳細を知らない
  - メモリー上の永続化可能な値として、`TKey` と `TValue` を保持する
- **Storage**
  - **値の表現**、**形式変換**、**実際のI/O** の詳細を知らない
  - メモリー上の値を管理する。（ここでは、`TKey` はただのキー、`PersistentEntry<TKey, TValue>` はただの値）
- **Serializer**
  - **値の表現**、**値の所有**、**実際のI/O** の詳細を知らない
  - メモリー上の `TValue` と、中間表現の `TData` を、相互変換する
- **Persistence**
  - **値の表現**、**値の所有**、**形式変換** の詳細を知らない
  - メモリー上の `TKey` と環境上の永続化 ID を相互変換する
  - メモリー上の `TValue` と環境上の永続化された実体を相互変換する（Serializer の変換機能を利用して中間表現で扱うことで、保存形式が変わっても契約は変わらない）
  - 環境上の永続化 ID や中間表現を元に、永続化された実体を読み書きする

#### Storage を用いた永続化の実装例

この例では、`.Standard` ライブラリを利用した単純な Storage の実装例を提示し、ライブラリの責務境界がどのような形で現れるかを示します。

<details> <summary> Port 定義の例 </summary>

```csharp
namespace MyApp.ExternalPorts
{
    // 省略エイリアス
    using MyEntry = PersistentEntry<TextStorageKey, TextStorageItem>;

    public interface IPersistentTextStorage :
        IStoragePort<TextStorageKey, MyEntry>
    {
    }

    public interface ITextStorageItemSerializerPort :
        ISerializerPort<Stream, TextStorageItem>
    {
    }

    public interface ITextStorageItemPersistencePort :
        IPersistencePort<TextStorageKey, TextStorageItem>
    {
    }
}
```

</details>

<details> <summary> Storage 実装の例 </summary>

```csharp
namespace MyApp.Externals
{
    // 省略エイリアス
    using MyEntry = PersistentEntry<TextStorageKey, TextStorageItem>;

    public sealed class PersistentTextStorage :
        Storage<TextStorageKey, MyEntry>,
        IPersistentTextStorage
    {
        protected override Result<MyEntry> CreateNewValue(TextStorageKey key)
        {
            var value = new TextStorageItem() { Text = $"Item {key}" };
            return new MyEntry(key, value);
        }

        protected override Result CanRemoveValue(TextStorageKey key, MyEntry value)
            => Result.Success;
    }
}
```

</details>

<details> <summary> Serializer 実装の例 </summary>

```csharp
namespace MyApp.Externals
{
    // TextSerializer<TValue> は、
    // TValue - string 間の相互変換によって、文字列ベースのシリアライズを行う。
    // ISerializerPort<string, TValue> の契約と同時に、
    // Stream を用いた ISerializerPort<Stream, TValue> の契約も満たす。
    public sealed class TextStorageItemSerializer :
        TextSerializer<TextStorageItem>,
        ITextStorageItemSerializerPort
    {
        public override Task<Result<TextStorageItem>> Deserialize(
            Result<string> inputText,
            Result<TextStorageItem> refValue)
        {
            return Task.FromResult(inputText
                .Then(text =>
                {
                    if (!refValue.Try(out var item, out _))
                    {
                        item = new TextStorageItem();
                    }

                    item.Text = text;
                    return item.AsResult();
                }));
        }

        public override Task<Result<string>> Serialize(
            Result<TextStorageItem> inputValue,
            Result<string> refText)
        {
            return Task.FromResult(inputValue
                .Then(item => item.Text.AsResult()));
        }
    }
}
```

</details>

<details> <summary> Persistence 実装の例 </summary>

```csharp
namespace MyApp.Externals
{
    // FilePersistence<TFileId, TValue> は、
    // ISerializerPort<Stream, TValue> をシリアライザーとして、
    // File名 - TFileId 間の相互変換によってローカルファイル永続化を行う。
    // ファイルパスは、virtual RootPath => Environment.CurrentDirectory を基準とする
    public sealed class TextStorageItemPersistence(
        ITextStorageItemSerializerPort serializer)
        : FilePersistence<TextStorageKey, TextStorageItem>(serializer),
        ITextStorageItemPersistencePort
    {
        public override string Extention => ".txt";

        public override TextStorageKey GetFileId(string fileName)
        {
            return new TextStorageKey(int.Parse(fileName));
        }

        public override string GetFileName(TextStorageKey fileID)
        {
            return $"{fileID.Value:00000}";
        }
    }
}
```

</details>

この実装によって、要素毎の読み書きは以下のような手順になります。

1. `Storage` は、キーに対応する `Entry` を単純に保持します。
2. 呼び出し側は、`Entry.Value` を操作します。この時、永続データは変更されません。
3. 必要な時点で、`PersistentEntry` と `PersistencePort` の機能を通じて、`Save()` や `Load()` などの永続化処理を実行します。

この時、`Entry.Value` はそのままメモリ上のキャッシュとして機能します。
通常の処理はキャッシュされた値に対して行われ、永続化先との同期だけが `PersistencePort` を用いて明示的に実行されます。
このように、永続化の操作を `Storage` 全体ではなく各 `Entry` に持たせることで、値の操作、キャッシュ、保存、復元などを、ひとつの `Entry` を中心とした一貫した処理として記述できます。
また、`Storage<TKey,TValue>` の内部実装における永続・非永続の区別も不要になります。

<details> <summary> この手順を実行するコード例 </summary>

```csharp
namespace MyApp.Interactions
{
    public sealed class InteractionUtility
    {
        public static async Task<Result> VerifySaveAndLoadAsync(
            IPersistentTextStorage storage,
            ITextStorageItemPersistencePort persistence,
            TextStorageKey key)
        {
            string savedText = $"SavedText:{Random.Shared.Next()}";

            // key に対応するキャッシュ済みの Entry を取得または作成する
            return await storage.GetOrCreate(key)

                // キャッシュ上の値を書き換える
                .Then(entry =>
                {
                    entry.Value!.Text = savedText;
                    return entry.AsResult();
                })

                // 同期処理の結果を引き継いで、非同期チェーンを開始する
                .StartAsync()

                // Entry.Value の値を永続化する
                .ThenAsync(async entry =>
                {
                    return await entry.Save(persistence)
                        // Save 成功時、同じ Entry を後続処理へ引き継ぐ
                        .ThenAsync(() => entry.AsResultAsync());
                })

                // 永続値を残したまま、キャッシュ上の値を空にする
                .ThenAsync(entry =>
                {
                    entry.Value!.Text = string.Empty;
                    return entry.AsResultAsync();
                })

                // 同じ Entry に永続値を読み戻す
                .ThenAsync(async entry =>
                {
                    return await entry.Load(persistence)
                        // Load 成功時、同じ Entry を後続処理へ引き継ぐ
                        .ThenAsync(_ => entry.AsResultAsync());
                })

                // 読み戻されたキャッシュ値を検証する
                .ThenAsync(entry =>
                {
                    var loadedText = entry.Value!.Text;

                    Result result = loadedText == savedText
                        ? Result.Success
                        : new InvalidOperationException(
                            $"Failed to save or load. " +
                            $"(key = {key.Value}, text = \"{loadedText}\")");

                    return result.StartAsync();
                });
        }
    }
}
```
</details>

<details> <summary> 💡 Tips: パフォーマンス上の理由で複数の値への同時アクセスが必要な場合 </summary>

> クエリ操作など、パフォーマンス上の理由で複数の値へのアクセスが必要な場合も存在します。
>
> この場合はまず、複数値操作用の `PersistencePort` を以下のような形で定義します。
> ```csharp
> public interface ITextStorageItemArrayPersistencePort :
>     IPersistencePort<TextStorageKey[], TextStorageItem[]>
> {
> }
> ```
>
> さらに、`StoragePort`（上の例では `IPersistentTextStorage`）に複数値操作のメソッドを定義します。
>
> ```csharp
> public interface IPersistentTextStorage :
>     IStoragePort<TextStorageKey, MyEntry>
> {
>     public Task<Result<TextStorageItem[]>> LoadAll(TextStorageKey[] keys,
>         IPersistencePort<TextStorageKey[], TextStorageItem[]> persistencePort);
>     ...
> }
> ```
>
> そして実装として、以下の実装を行います。
> - `TextStorageItemArrayPersistencePort` は、永続データの同時操作を実装する
>   - `Task<Result<TextStorageItem[]>> Load(TextStorageKey[] ids, Result<TextStorageItem[]> oldValue)` など
>   - 通常の `PersistencePort` の実装と同様に `ITextStorageItemSerializerPort` も利用可能
> - `PersistentTextStorage` は、 `TextStorageItemArrayPersistencePort` を利用してキャッシュと永続データを連携させる
>
>   ```csharp
>   public async Task<Result<TextStorageItem[]>> LoadAll(
>           TextStorageKey[] keys,
>           IPersistencePort<TextStorageKey[], TextStorageItem[]> persistencePort)
>   {
>       try
>       {
>           var items = keys.Select(key =>
>               GetOrCreate(key).Try(out var value, out var error) ?
>               value.Value! : throw error
>           );
>           return await persistencePort.Load(keys, items.ToArray())
>               .ThenAsync(async loaded =>
>               {
>                   if (loaded.SequenceEqual(items))
>                       return loaded.AsResult();
>
>                   return new InvalidOperationException(
>                       "Multi-value persistence must return the" +
>                       " cached value instances provided as oldValue.");
>               });
>       }
>       catch (Exception e)
>       {
>           return e;
>       }
>   }
>    ```

</details>

### Silent External

ソースコード: [`ISilentExternalPort.cs`](../InteractionFlow.Core/ExternalPorts/SilentExternalPorts/ISilentExternalPort.cs)、[`SilentExternal.cs`](../InteractionFlow.Core/Externals/SilentExternals/SilentExternal.cs)

Silent External は、Operation / Reaction のような「User との相互作用」や、
Storage のような「System 内での記録」のいずれも目的としないような外部機能を提供します。
この機能分類は事実上、「その他の機能」の受け口として働きますが、だからこそ可能な限り小さく保つ必要があります。
Silent External として定義する前に、Operation / Reaction / Storage や、それらの状態として定義する可能性を検討することが推奨されます。

```csharp
namespace MyApp.ExternalPorts
{
    public interface ILogPort : ISilentExternalPort
    {
        void Write(string message);
    }
}

namespace MyApp.Externals
{
    public sealed class DebugLog : SilentExternal, ILogPort
    {
        public override void ForceResetMemoryState() { }

        public void Write(string message)
            => Debug.WriteLine(message);
    }
}
```

## 体験と相互作用のフロー (Interaction, SystemFlow) <a id="interaction-systemflow"></a>

Function が相互作用に必要な機能を表すのに対し、`Interaction` と `SystemFlow` は、
その機能を User と System の関係の時間的な流れとして構成します。

```text
Context
  ↓
SystemFlow ── 1つ以上の Interaction ── 1つ以上の Function Port ── Function External
  │               │
  │               └─ 相互作用の段階を一つ進める（最後に必ず Reaction を実行して終了）
  └─ 相互作用の順序・分岐・反復を構成する
  ↓
next Context
```

この実行経路は `Context Loop` の System 側を表します。
User 側では、この経路を体験として自身に取り込み、次の動作を決定することが期待されます。

`Interaction` は相互作用の段階を一つ進め、
`SystemFlow` は一つ以上の Interaction を通じて System が User との関係を構築する単位を実現します。
どちらも `IFlowContext` が提供する現在を受け取り、結果として次の相互作用へ影響する状態を形づくります。

### Interaction

ソースコード: [`IInteraction.cs`](../InteractionFlow.Core/Interactions/IInteraction.cs)、[`Interaction.cs`](../InteractionFlow.Core/Interactions/Interaction.cs)

`Interaction` は Function Port の呼び出しと、必要に応じた Domain の計算を組み合わせ、
System 内部の目的を一段進める実行単位です。複数の Function 呼び出しを含んでも、
`SystemFlow` からは一つの相互作用の段階として扱われます。

派生 Interaction は、中心処理として次を実装します。

```csharp
protected abstract Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context);
```

例えば入力を受け、対応する反応を返す Interaction は、次のように Port の組み合わせとして表せます。

```csharp
namespace MyApp.Interactions
{
    public sealed class EchoInteraction(
        ITextOperation operation,
        ITextReaction reaction,
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort)
        : Interaction(exceptionPort, cancellationPort, operation, reaction)
    {
        // ExecuteCoreAsync の中で throw した例外は、親の Interaction class が
        // exceptionPort / cancellationPort を利用してハンドリングしてくれる。
        // Interaction.CancellationPort 等のプロパティを利用して自分で解決することも可能。
        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            context.Cancellation.GetToken().ThrowIfCancellationRequested();
            var text = await operation.ReadAsync(context);

            context.Cancellation.GetToken().ThrowIfCancellationRequested();
            return await reaction.ReactAsync(context, text);
        }
    }
}
```

この例では、Interaction は、Function Port を通じて、入力と反応を相互作用の一つの段階としてどのように結び付けるかを決定しています。
具体的なテキストの取得元や、表示先は、DI で注入される Function External が決めます。

より一般には、Interaction は、一つ以上の Reaction / Operation / Storage / Silent External を含む「相互作用の段階」を実装することが出来ます。
User からの入力を必要としない Interaction では Operation を含まない構成も可能です。
ただし、終了状態である `FlowEndToken` は Reaction 系 API からのみ取得できる `ReactionEnd` を必要とします。
これは、Interaction を Reaction で終了してユーザー体験として閉じるために、ライブラリ利用者が守る必要がある制約を型として表現したものです。
ただし、ライブラリ側では、Reaction 系 API が返す `ReactionEnd` が「System が実際に反応を実行したこと」を保証しないため、ライブラリ利用者がこのことを保証する責務を持ちます。

Interaction の実装粒度は、「相互作用の意味」として独立した単位であることが目安になりますが、最終的な裁定はライブラリ利用者に委ねられます。

`Interaction` 基底クラスは、以下の内容を基本動作として実装しています。

- `ExecuteCoreAsync` 実行前のキャンセル確認
- `ExecuteCoreAsync` / `OnCancellation` をキャンセル時に待機するための登録
- `ExecuteCoreAsync` 内でスローされた `OperationCanceledException` の `Interaction.CancellationPort` への委譲
- `ExecuteCoreAsync` 内でスローされたその他の例外の `Interaction.ExceptionPort` への委譲
- `ReactionEnd` と実行時の `IFlowContext` からの `FlowEndToken` 作成

これにより、Interaction 前のキャンセルを確実に行うこと、例外への反応を自動的に委譲する、`FlowEndToken.LastContext` が現在の Interaction の実行境界を表すことなどを保証します。
ただし、ExceptionPort / CancellationPort 自身が例外を送出する場合は、呼び出し側で捕捉されない限り、そのまま未処理例外として上位へ伝播し、`FlowEndToken` も作成されません。

結果として、派生 Interaction は、例外・キャンセルの反応を直接実装せず、固有の Function の組み合わせへ集中できます。

<details>
<summary>💡 Tips: Interaction.NestedExecuteAsync について</summary>

> 現在の API では、ネストした Interaction を `Interaction.NestedExecuteAsync` で実行することができます。
> この実行経路では、`Interaction.NestedFlowContext : IFlowContext` を用いて、親の `IFlowContext` の文脈値を参照しつつ、`CancellationObject` は新しく作成した物を使用します。
> ただし、親の `IFlowContext` へのキャンセルトリガーは、`CancellationToken.Register()` を利用して `NestedFlowContext` にも伝播されます。
>
> 独立したキャンセル制御を使用する主な理由は、キャンセル待機処理の循環待機を避けるためです。
> 親 Interaction と子 Interaction が同じ CancellationObject を使用すると、両方のキャンセル対象タスクが同じ待機一覧へ登録されます。
> その状態で子のキャンセル処理が一覧全体の完了を待つと、循環によるデッドロックが発生する可能性があります。

</details>

### SystemFlow

ソースコード: [`ISystemFlow.cs`](../InteractionFlow.Core/SystemFlows/ISystemFlow.cs)、[`SystemFlow.cs`](../InteractionFlow.Core/SystemFlows/SystemFlow.cs)

`ISystemFlow<TContext>` は、指定された `IFlowContext` 実装型で SystemFlow を実行する契約です。

```csharp
Task<FlowEndToken> ExecuteAsync(TContext context);
```

SystemFlow は、Interaction を一つの段階として実行するだけでなく、複数の相互作用を合成し、体験の流れを構築します。

`SystemFlow<TContext>` 基底クラスは、基本動作として、派生型の `ExecuteCoreAsync` が返した終了結果を `ExecuteAsync` に渡された `IFlowContext` インスタンスへ結び直します。
これにより、`FlowEndToken.LastContext` が、現在の SystemFlow の実行境界を表すことを保証します。


```csharp
protected override async Task<FlowEndToken> ExecuteCoreAsync(IFlowContext context)
{
    FlowEndToken end;

    end = await helloInteraction.ExecuteAsync(context);

    while (!end.HasException && !IsExitRequested(context))
    {
        end = await echoInteraction.ExecuteAsync(context);
    }

    if (!end.HasException)
    {
        end = await goodbyeInteraction.ExecuteAsync(context);
    }

    return end;
}
```

### Context の所有権

`SystemFlow.ExecuteAsync` と `Interaction.ExecuteAsync` は、渡された `IFlowContext` インスタンスの文脈値を
破棄しません。
文脈値を破棄する責務は、その `IFlowContext` を作成した呼び出し側が保持します。
これにより、作成した `IFlowContext` インスタンスは、必要に応じて次の SystemFlow などへ再利用することができます。
同様に、`FlowEndToken.LastContext` も `IFlowContext` の所有権を持たず、単純な参照を意味します。
`FlowEndToken.LastContext` は、フロー実行関数の引数と同一であることを原則としているため、通常の利用範囲では所有権が曖昧になることはありません。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./PHILOSOPHY.md) |
[計算モデルとして](./COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./LIBRARY_IMPLEMENTATION_DETAIL.md) |
