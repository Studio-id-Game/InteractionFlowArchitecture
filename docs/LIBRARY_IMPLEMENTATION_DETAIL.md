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
- [値と処理結果の意味論 (Result, ReactionEnd, InteractionEnd, Entry)](#entry-result)
- [文脈の構成 (Context)](#context)
- [機能の分離 (Functions)](#functions)
- [体験と相互作用のフロー (Interaction, SystemFlow)](#interaction-systemflow)

## システムの組み立て (System Flow Builder) <a id="system-flow-builder"></a>

`Program` は、実行環境を選び、依存オブジェクトを組み立て、`IFlowContext` インスタンスを渡して `SystemFlow` を実行します。

System Flow Builder の設計意図と、Builder、Handler、Scope に分けている理由については、
[System Flow Builder の設計](./SYSTEM_FLOW_BUILDER.md) を参照してください。

このライブラリでは、複数のSystemFlow間での依存スコープの共有などを想定して、
自身で解決できない依存を親 Scope から順に探索できる依存スコープを実装しています。

このスコープは、Builder によって解決・構築され、Handler によって寿命が管理されます。

| 型 | 役割 |
| --- | --- |
| `ScopeBuilder` | 登録情報から依存解決スコープを一つ構築する |
| `ScopeHandler` | 構築済みスコープを保持し、自身で解決できない場合は親 Scope を順に探索する。破棄時は自身のスコープだけを破棄する |
| `SystemFlowBuilder<TContext>` | SystemFlow と専用スコープを構築する |
| `SystemFlowHandler<TContext>` | SystemFlow を実行対象として保持し、破棄時は専用 `ScopeHandler` を破棄する |


Builder は一度 Build すると再利用できません。子スコープで解決できない依存は親から解決されます。
Handler を破棄すると自身のスコープは無効になりますが、親スコープと外部から渡された
`IFlowContext` インスタンスは破棄しません。
親 Scope を先に破棄した場合、子 Scope がその親を探索した時点で例外になります。
`SystemFlowHandler` は、保持している SystemFlow 自体へ `Dispose` を直接呼び出しません。

### 基本的な手続き
```text
Program
  ├─ ScopeBuilder に Port 実装と Interaction を登録する
  ├─ ScopeBuilder から ScopeHandler を構築する
  ├─ SystemFlowBuilder に追加・上書きする Port 実装と Interaction を登録する
  ├─ SystemFlowBuilder から、ScopeHandler を fallback 探索先とする SystemFlowHandler を構築する
  └─ SystemFlowHandler に IFlowContext を渡して実行する
```

### 実際のプログラム例

以下は、実際に単純な依存関係を構築して SystemFlow を実行する例や、
依存スコープに fallback 探索先を指定するコード例です。

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

  // sharedScopeHandler を fallback 探索先とする ScopeHandler の構築
  using var localScopeHandler = localScopeBuilder.BuildScope(sharedScopeHandler);

  // SystemFlow 独自の、誰にも共有されない依存解決の登録
  var flowBuilderB =  new SystemFlowBuilder<IFlowContext>();
  flowBuilderB
      .UseFunction<IMyFunctionC, MyFunctionC>()
      .UseInteraction<MyInteractionB>();

  // localScopeHandler を fallback 探索先とする SystemFlowHandler の構築
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
各ノードの表示には `IDependencyNode.Name` を使用し、既定では実行時の型名を表示します。

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

現在の再帰経路に同じノードインスタンスが再登場した場合は、表示名へ既定の `@Cycle` を付け、
その枝の再帰だけを停止します。比較には値等価性ではなく参照同一性を使用します。
別の枝から参照される共有ノードは循環とは扱わず、それぞれの枝で表示します。

#### Samples.HelloDoor での実際の出力

> - DoorSystemFlow
>   - OperateDoor
>     - ConsoleExceptionHandling
>     - ConsoleCancellationHandling
>     - ConsoleDoorOperation
>     - ConsoleDoorReaction

#### 依存ノードツリーの目的

このツリーは、実行時に解決された依存を観察し、以下の作業を支援します。

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

Analyzer は、認識できる依存ノード宣言と `Dependency` 列挙の整合性を検査します。
これにより、以下のような列挙漏れを検出できます。

- リファクタリングで追加した依存を `Dependency` へ含め忘れた場合、Analyzer で検出できる
- 派生クラスが追加した依存を基底クラスへ渡し忘れた場合、Analyzer で検出できる

この検査は、DI の登録一覧全体、動的なサービス取得、実行途中で追加される依存、
`IDependencyNode` として表現されない依存まで保証するものではありません。

詳細は、[保証と制約と責務](./LIBRARY_IMPLEMENTATION.md#guarantees) を参照してください。

## 値と処理結果の意味論 (Result, ReactionEnd, InteractionEnd, Entry) <a id="entry-result"></a>

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
| `InteractionEnd` | `Interaction`/`SystemFlow` の結果に意味を持たせる |
| `Entry<TValue>` | 保持する値に意味を持たせる |

### Result

Result の契約、各拡張の使い分け、例外を Result 化する責務は [Result API](./RESULT.md) を基準とします。

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

`ReactionEnd` は Reaction が確定した `Result` を保持します。
相互作用は明示的なリアクションによって動作を終了する、という生成規則を表します。

- `Result` プロパティで成功・失敗の検査や既存の連結 API を利用できます。
- 生成は引き続き `Reaction` / `IReactionPort` の protected `GetEnd` に限定します。
  任意の Result から公開ファクトリーや暗黙変換で生成することはできません。
- `GetEnd(Result)` は既存の失敗 Result を再ラップせず保持します。
  引数なし、および `Exception?` を受ける既存の GetEnd も利用できます。
- `HasException` は `!Result.Try(out _)` です。
- `Exception` は ResultException の一段のラッパーを除いた原因です。
  InnerException を持たない ResultException はそのまま返します。
- `HasCanceled` はその原因が OperationCanceledException の場合だけ true です。
  AggregateException の内部は探索しません。

```csharp
// Reaction 派生型内
ReactionEnd Complete(Result result) => GetEnd(result);

// 利用側
if (!end.Result.Try(out var error))
{
    // error は ResultException。元の原因は InnerException に保持されます。
}
```

成功結果は、User が実際に反応を観測したことまでは保証しません。
また、途中の Reaction の結果を受け取っただけで、体験全体が終了するわけではありません。
次の Interaction を実行するか、体験を終了するかは SystemFlow が判断します。

### InteractionEnd

ソースコード: [`InteractionEnd.cs`](../InteractionFlow.Core/Entities/Contexts/InteractionEnd.cs)

`InteractionEnd` は ReactionEnd に基づく Interaction / SystemFlow の完了を表します。
Context は保持せず、LastContext と境界ごとの Context 結合は廃止しました。

- `End` は Reaction が生成した ReactionEnd です。
- `Result` は `End.Result` を公開します。既存の補助プロパティも End に委譲します。
- Interaction の終了境界で ReactionEnd から生成します。
  公開コンストラクターや Result からの直接変換は設けません。
- SystemFlow は子が返したトークンをそのまま返します。再生成や Context の結合は行いません。
- Context の所有権・寿命は呼び出し側が管理します。

```csharp
// IInteraction 実装内
return IInteraction.GetEnd(reactionEnd);

// SystemFlow 実装内
return await interaction.ExecuteAsync(context);
```

Interaction が未処理例外を専用 Port に委譲する契約は維持します。
Port 自体の例外は上位へ伝播し、その経路ではトークンを生成しません。
SystemFlow の送出例外も自動で Result 化しません。

### Entry

ソースコード:
[`Entry.cs`](../InteractionFlow.Core/Entities/Entry.cs),
[`RefEntry.cs`](../InteractionFlow.Core/Entities/RefEntry.cs)

`Entry<TValue>` は、保持する値に、型としての解決や更新可能性などの意味を持たせるための基底クラスです。

これは `Result` / `ReactionEnd` / `InteractionEnd` と同様に値の意味を表しますが、
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
  [`PersistentEntry.cs`](../InteractionFlow.Core/ExternalPorts/PersistentEntry.cs),
  [`PersistentEntryExtensions.cs`](../InteractionFlow.Core/ExternalPorts/PersistentEntryExtensions.cs)

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
例えば `FlowContext` が Entry の循環参照を検出した場合や、破棄後に呼ばれた場合は例外になります。

### `FlowContext`

ソースコード: [`FlowContext.cs`](../InteractionFlow.Core/Entities/Contexts/FlowContext.cs)

FlowContext は文脈値の構成と探索、およびキャンセル制御を提供します。
従来の ScopedFlowContext は統合・削除しました。

```csharp
using var root = new FlowContext().With(new MyText("parent"));
using var child = new FlowContext(root).With(new MyText("child"));
```

引数なしでは独自の CancellationObject を持つルートを作成し、親ありでは親と同じ CancellationObject を共有します。
親の値は変更せず、子インスタンスに追加値を重ねます。

探索順序は次のとおりです。

1. 自分に追加した値を新しい順に探索する。
2. 見つからなければ派生型の TryGetCore へ委譲する。
3. 基本の TryGetCore は親があれば親を探索し、ルートなら Cancellation を提供する。

追加値は派生型の独自値や親の同型値を上書きします。
親で上書きされた値も親の TryGet 経由で引き継ぎます。
CancellationObject 型の追加値も探索では優先されますが、Cancellation プロパティの共有先は変更しません。
Entry の解決は従来と同じく再帰的で、不在なら次候補へ進み、それ以外の解決失敗は例外として伝播します。

公開の TryGet / With / Dispose は非 virtual です。
派生型は TryGetCore / DisposeCore を override し、共通の取得・破棄契約を維持します。

```csharp
protected override bool TryGetCore<T>([MaybeNullWhen(false)] out T value)
{
    if (this is T self) { value = self; return true; }
    return base.TryGetCore(out value);
}
```

Dispose は先に破棄済みとして追加値と親への探索参照を解放し、その後 DisposeCore を一度だけ呼びます。
フックが例外を送出しても破棄済み状態を維持し、その例外は呼び出し側へ伝播します。
破棄後は With と TryGet が ObjectDisposedException になります。
Cancellation は破棄後も参照できます。派生型の独自プロパティへのアクセスは禁止しません。

親と With で追加した値・Entry・Cancellation は破棄しません。
値の所有権と派生型自身の資源の解放は、所有者が管理します。
変更・探索と Dispose の並行実行はサポートしません。

### `CancellationObject`

ソースコード: [`CancellationObject.cs`](../InteractionFlow.Core/Entities/Contexts/CancellationObject.cs)

IFlowContext.Cancellation は、要求の通知と登録 Task の終了待機を管理する CancellationObject を提供します。

| 関数 | 役割 |
| --- | --- |
| `GetToken()` | 現在のトークンを取得する。待機中もキャンセル済み状態を維持する |
| `AddCancelableTask(Task)` | 待機対象を登録し、登録解除用の IDisposable を返す |
| `Cancel()` | 現在の対象へ同期的に通知する。Task の終了は待たない |
| `WaitForTasksAndResetCancellationAsync()` | 確定した全登録 Task を待ち、失敗を集約してからリセットする |

```csharp
var task = DoWorkAsync(cancellation.GetToken());
using var registration = cancellation.AddCancelableTask(task);
await task;
```

処理の所有者が結果を受け取り、登録の寿命を閉じます。登録スコープの Dispose は Task 自体を停止・破棄しません。
待機開始後の登録は拒否しますが、登録解除によって確定済みの待機対象を取り消すことはありません。
即時・非同期の完了経路で同じ失敗集約を行い、Faulted Task の例外を保持します。
通知中の Source は最後の Cancel 呼び出しの終了まで破棄を遅延し、コールバックを内部ロック外で実行します。

Interaction は完了通知 Task を登録し、本体と OnCancellation の完了を通知してから Port の処理へ進みます。
登録スコープは Port の処理後に解除します。SystemFlow はその Interaction の終了結果から次の相互作用を決めます。

詳細な寿命・失敗・競合・移行の契約は [CancellationObject](./CANCELLATION.md) を参照してください。

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

ソースコード: [`IOperationPort.cs`](../InteractionFlow.Core/ExternalPorts/IOperationPort.cs)、[`Operation.cs`](../InteractionFlow.Core/Externals/Operation.cs)

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

ソースコード: [`IReactionPort.cs`](../InteractionFlow.Core/ExternalPorts/IReactionPort.cs)、[`Reaction.cs`](../InteractionFlow.Core/Externals/Reaction.cs)

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

ソースコード: [`IStoragePort.cs`](../InteractionFlow.Core/ExternalPorts/IStoragePort.cs)、[`Storage.cs`](../InteractionFlow.Core/Externals/Storage.cs)

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

`IStoragePort` は Storage の操作契約を表しますが、`IDisposable` の実装は要求しません。
標準基底実装の `Storage<TKey, TValue>` は `IDisposable` を実装し、`GetOrCreate` で登録した値を所有します。
登録した値が `IDisposable` の場合、次の API によって登録解除と破棄を制御します。

| API | `CanRemoveValue` | 登録解除 | 値の破棄 | 主な用途 |
| --- | --- | --- | --- | --- |
| `RemoveAndDispose` | 対象値を検査 | 成功時に行う | 行う | 1件を通常解放する |
| `RemoveWithoutDispose` | 対象値を検査 | 成功時に行う | 行わない | 呼び出し側が保持済みの参照と破棄責務を引き受ける |
| `ClearAndDispose` | 全件を先に検査 | 全件検査成功後に行う | 行う | 全件を通常解放する |
| `ClearWithoutDispose` | 全件を先に検査 | 全件検査成功後に行う | 行わない | 全件の登録だけを解除する |
| `ForceResetMemoryState` | 行わない | 行う | 行う | 強制リセットする |
| `Dispose` | 行わない | 行う | 行う | Storage の所有状態を解放する |

`ClearAndDispose` と `ClearWithoutDispose` は、すべての値について `CanRemoveValue` を先に実行します。
削除できない値がある場合は、その失敗 `Result` が持つ例外だけを `AggregateException` に集約した
失敗 `Result` を返し、登録状態を変更しません。
`CanRemoveValue` 自体が予期せず例外を送出した場合、その例外は集約されずに呼び出し側へ伝播します。

`ClearAndDispose`、`ForceResetMemoryState`、`Dispose` は、値の破棄で発生した例外を集約して送出します。
この場合も、すべての値について破棄を試み、Storage の登録状態を初期化します。
`RemoveAndDispose` は登録解除後に値を破棄するため、値の破棄で例外が発生しても、
その値はすでに Storage の登録から外れています。

`Storage<TKey, TValue>` が DI Scope で破棄可能なサービスとして追跡されている場合は、
Scope の破棄によって `Storage.Dispose()` も実行されます。
これは、`IDisposable` を実装しない独自の `IStoragePort` 実装にまで適用される契約ではありません。
また、現在の `Storage.Dispose()` は呼び出し後の再利用を明示的には拒否しないため、
本文では「破棄後は利用不可」という追加の契約を設けません。

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

ソースコード: [`Storage.cs`](../InteractionFlow.Core/Externals/Storage.cs)、[`PersistentEntry.cs`](../InteractionFlow.Core/ExternalPorts/PersistentEntry.cs)、[`IPersistencePort.cs`](../InteractionFlow.Core/ExternalPorts/IPersistencePort.cs)、[`ISerializerPort.cs`](../InteractionFlow.Core/ExternalPorts/ISerializerPort.cs)

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
    // GetCurrentRootPath() を各操作で一度だけ取得・絶対パス化して基準とする
    public sealed class TextStorageItemPersistence(
        ITextStorageItemSerializerPort serializer)
        : FilePersistence<TextStorageKey, TextStorageItem>(serializer, ".txt"),
        ITextStorageItemPersistencePort
    {
        protected override TextStorageKey GetIdFromRelativePathWithoutExtension(string relativePath)
        {
            return new TextStorageKey(int.Parse(relativePath));
        }

        protected override string GetRelativePathWithoutExtension(TextStorageKey fileID)
        {
            return $"{fileID.Value:00000}";
        }
    }
}
```

</details>

この実装によって、要素毎の読み書きは以下のような手順になります。

ファイル保存の失敗時保証、Directory の拡張点、旧 API からの移行は
[Standard FileSystem](./FILESYSTEM.md) を参照してください。

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

<details> <summary> 💡 Tips: パフォーマンス上の理由で複数の値をまとめて読み込む場合 </summary>

ソースコード: [`PersistentEntryExtensions.cs`](../InteractionFlow.Core/ExternalPorts/PersistentEntryExtensions.cs)

クエリ操作など、複数の永続値を一度に読み込む必要がある場合は、
配列を扱う `PersistencePort` と `PersistentEntry[].LoadAll` を利用できます。
この機能は、利用側が Entry 配列を直接組み立てるのではなく、
用途固有の `IPersistentTextStorage` に定義してアクセスポイントにすることを、
このライブラリにおけるベストプラクティスとします。

このカスタム例では、上の基本例にある `IPersistentTextStorage` の定義を、
複数値読み込みのアクセスポイントを持つ次の定義へ置き換えます。

```csharp
namespace MyApp.ExternalPorts
{
    // 省略エイリアス
    using MyEntry = PersistentEntry<TextStorageKey, TextStorageItem>;

    public interface IPersistentTextStorage :
        IStoragePort<TextStorageKey, MyEntry>
    {
        Task<Result<TextStorageItem?[]>> LoadAll(
            TextStorageKey[] keys,
            ITextStorageItemArrayPersistencePort persistencePort);
    }

    public interface ITextStorageItemArrayPersistencePort :
        IPersistencePort<TextStorageKey[], TextStorageItem?[]>
    {
    }
}
```

Storage 実体側では、キーに対応する Entry を解決し、
`PersistentEntry[].LoadAll` へ処理を委譲します。

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

        public async Task<Result<TextStorageItem?[]>> LoadAll(
            TextStorageKey[] keys,
            ITextStorageItemArrayPersistencePort persistencePort)
        {
            try
            {
                var entries = new MyEntry[keys.Length];

                for (int i = 0; i < keys.Length; i++)
                {
                    if (!GetOrCreate(keys[i]).Try(out var entry, out var error))
                    {
                        return error;
                    }

                    entries[i] = entry;
                }

                return await entries.LoadAll(persistencePort);
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}
```

利用側は Entry 配列を扱わず、再定義した Storage Port をアクセスポイントとして利用します。

```csharp
public static async Task<Result<TextStorageItem?[]>> LoadAllAsync(
    IPersistentTextStorage storage,
    ITextStorageItemArrayPersistencePort arrayPersistencePort)
{
    TextStorageKey[] keys =
    [
        new(1),
        new(2)
    ];

    return await storage.LoadAll(keys, arrayPersistencePort);
}
```

`IPersistentTextStorage.LoadAll` の実装は、各キーに対応する Entry を `GetOrCreate` で解決し、
`PersistentEntry[].LoadAll` へ委譲します。
これにより、キーとキャッシュ済み Entry の対応付けを Storage 実装内に保ち、
利用側には用途固有の Storage Port だけを公開できます。
汎用契約である `IStoragePort` 自体には、複数値永続化の責務を追加しません。

なお、このカスタム実装は `GetOrCreate` による Storage への登録を先に行うため、
バッチ全体として原子的ではありません。途中の `GetOrCreate` または後続の読み込みが失敗しても、
その時点までに新規作成・登録された Entry は Storage に残り、この登録はロールバックされません。
原子性が必要な場合は、用途固有の Storage 実装で事前検証、一時領域、または明示的なロールバックを設計します。
この登録副作用は、以下に示す `PersistentEntry[].LoadAll` の値更新契約とは別です。

委譲先の `PersistentEntry[].LoadAll` は、各 Entry の `PersistenceId` と現在値を
入力順の配列として Persistence へ渡し、読み込み結果を同じ位置の Entry へ対応させます。

- 読み込み結果が非 `null` の要素は、対応する Entry の現在値を更新する
- 読み込み結果が `null` の要素は、対応する Entry の現在値を維持する
- 読み込み結果の要素数が Entry 数と異なる場合は、Entry を更新せず失敗 `Result` を返す
- Persistence が失敗 `Result` を返した場合は、Entry を更新しない

Persistence が同じ参照を返す必要はありません。
また、同じ Entry に対する複数の `LoadAll` を並行実行した場合の完了順は制御しないため、
逐次化、Entry の分離、または呼び出し側での同期が必要です。

</details>

### Silent External

ソースコード: [`ISilentExternalPort.cs`](../InteractionFlow.Core/ExternalPorts/ISilentExternalPort.cs)、[`SilentExternal.cs`](../InteractionFlow.Core/Externals/SilentExternal.cs)

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
ただし、終了状態である `InteractionEnd` は Reaction 系 API からのみ取得できる `ReactionEnd` を必要とします。
これは、Interaction を Reaction で終了してユーザー体験として閉じるために、ライブラリ利用者が守る必要がある制約を型として表現したものです。
ただし、ライブラリ側では、Reaction 系 API が返す `ReactionEnd` が「System が実際に反応を実行したこと」を保証しないため、ライブラリ利用者がこのことを保証する責務を持ちます。

Interaction の実装粒度は、「相互作用の意味」として独立した単位であることが目安になりますが、最終的な裁定はライブラリ利用者に委ねられます。

`Interaction` 基底クラスは、以下の内容を基本動作として実装しています。

- `ExecuteCoreAsync` 実行前のキャンセル確認
- 本体と `OnCancellation` の完了を表す通知 Task をキャンセル待機一覧へ登録
- 本体 Task のキャンセル例外に対して `OnCancellation` を待ち、完了通知を確定してから `Interaction.CancellationPort` へ委譲
- `ExecuteCoreAsync` 内でスローされたその他の例外の `Interaction.ExceptionPort` への委譲
- `ReactionEnd` からの `InteractionEnd` 作成

これにより、Interaction 前のキャンセル確認、例外への反応の委譲、Reaction に由来する終了結果の生成を行います。
ただし、ExceptionPort / CancellationPort 自身が例外を送出する場合は、呼び出し側で捕捉されない限り、そのまま未処理例外として上位へ伝播し、`InteractionEnd` も作成されません。

結果として、派生 Interaction は、例外・キャンセルの反応を直接実装せず、固有の Function の組み合わせへ集中できます。

`OnCancellation` の実行は `ExecuteAsync` の一つの経路で管理します。
同じ本体 Task を別の補助 Task が監視する構成ではなく、TaskCompletionSource の Task で
「本体とキャンセル後始末が完了したこと」を待機側へ知らせます。

```text
本体 Task のキャンセル例外
  → OnCancellation を待つ
  → 完了通知を確定する（後始末の失敗は通知 Task に保持）
  → CancellationPort の前処理
  → WaitForTasksAndResetCancellationAsync
  → キャンセル後の反応
```

完了通知を確定してから Port へ進むため、WaitForTasksAndResetCancellationAsync が自分自身の未完了通知を待つことを避けます。
登録スコープは Port の処理まで保持し、正常・失敗を問わず最後に解除します。
Port が待機・集約を完了できず後始末の失敗を保持しない場合は、元のキャンセルと Port の失敗も含めて伝播します。
待機結果へ失敗を渡した後の Port による回復・変換は尊重します。
既に結果・例外へ含まれている失敗は重複集約しません。

本体の通常例外では完了通知を成功にし、元の例外は ExceptionPort にだけ委譲します。
これにより、後のキャンセル待機で本体の例外を重複報告しません。

開始前キャンセルと、ExecuteCoreAsync が Task を返す前に同期的に投げたキャンセル例外では、
既存動作どおり OnCancellation を呼びません。ReactionEnd として返されたキャンセルも例外経路とは区別します。
OnCancellation が失敗した場合（その Task のキャンセル完了も含む）は通知 Task を faulted とし、
元の本体キャンセルは引き続き CancellationPort へ渡します。

従来は OnCancellation と CancellationPort の前処理が並行し得ましたが、現在は上記の順序を明示しています。
OnCancellation 内から同じ CancellationObject の WaitForTasksAndResetCancellationAsync を待つと、
自身の完了通知を待つ循環になるため、このフックでは後始末だけを行います。
Interaction 同士の共有 Context による直接ネストの循環待機を解消する変更ではありません。


<details>
<summary>💡 Tips: Interaction の合成と循環待機について</summary>

> 旧 API の `Interaction.NestedExecuteAsync` は削除しました。
> 相互作用の順序・分岐・反復は SystemFlow で構成してください。
> 共有 Context で子の ExecuteAsync を直接呼ぶ置換は、下記の循環待機を再導入するため代替にはなりません。
>
> 独立したキャンセル制御を使用する主な理由は、キャンセル待機処理の循環待機を避けるためです。
> 親 Interaction と子 Interaction が同じ CancellationObject を使用すると、両方のキャンセル対象タスクが同じ待機一覧へ登録されます。
> その状態で子のキャンセル処理が一覧全体の完了を待つと、循環によるデッドロックが発生する可能性があります。
> SystemFlow 同士の逐次ネストは、自身のタスクをキャンセル待機一覧へ登録しないため、同じ循環を作りません。

</details>

### SystemFlow

ソースコード: [`ISystemFlow.cs`](../InteractionFlow.Core/SystemFlows/ISystemFlow.cs)、[`SystemFlow.cs`](../InteractionFlow.Core/SystemFlows/SystemFlow.cs)

`ISystemFlow<TContext>` は、指定された `IFlowContext` 実装型で SystemFlow を実行する契約です。

```csharp
Task<InteractionEnd> ExecuteAsync(TContext context);
```

SystemFlow は、Interaction を一つの段階として実行するだけでなく、複数の相互作用を合成し、体験の流れを構築します。

`SystemFlow<TContext>` 基底クラスは、派生型の `ExecuteCoreAsync` が返した終了結果をそのまま返します。
終了結果に Context は保持しません。


```csharp
protected override async Task<InteractionEnd> ExecuteCoreAsync(IFlowContext context)
{
    InteractionEnd end;

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
`InteractionEnd` は Context の参照を保持しません。

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./PHILOSOPHY.md) |
[計算モデルとして](./COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./LIBRARY_IMPLEMENTATION_DETAIL.md) |
