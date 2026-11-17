# Result API

`InteractionFlow.Core.ResultAPI.Entities` の `Result` は、局所的な処理の成功・失敗を明示して連結するための API です。
この文書を、Result の契約と例外の扱いを確認する基準とします。
`ReactionEnd` はこの Result を保持し、Reaction による終了結果の生成規則を付与します。
`InteractionEnd` はそれに基づく Interaction / SystemFlow の完了を表し、Context は保持しません。
詳細は [Reaction とフローの結果](./REACTION_RESULTS.md) を参照してください。

## 成功値と失敗

- `Result` は値を持たない成功・失敗です。`Result.Success` と `default(Result)` は成功です。
- `Result<T>` は非 null の成功値、または例外による失敗です。
- `Result<T, TError>` は失敗時に例外と非 null の失敗値を保持します。
- 値付き Result の `default` は未初期化です。結果を検査する操作やチェーンは `ResultException` を送出します。
- 値は暗黙変換または `AsResult()` で成功結果にできます。null と Exception 派生型は成功値にできません。
  それらをデータとして保持する必要がある場合は、非 null のコンテナー型で包みます。
- Exception は失敗結果へ暗黙変換できます。失敗を作るために throw / catch する必要はありません。
- 失敗は同じ名前空間の [`ResultException`](../InteractionFlow.Core/ResultAPI/Entities/ResultException.cs) で包まれ、元の例外は InnerException に保持されます。
  既存の ResultException を伝播するときは二重に包みません。
  DEBUG ビルドでは ResultCreationStackTrace で失敗結果の生成位置を確認できます。

```csharp
Result<int> ParseCount(string text)
{
    return int.TryParse(text, out var count)
        ? count.AsResult()
        : new FormatException("Invalid count.");
}
```

`IsSuccess` で状態を調べ、`HasValue(out value)` で成功値、`HasError(out error)` で例外を取得します。
`Result<T, TError>` の `HasError(out error, out errorValue)` は失敗値も取得します。
`WithoutValue` は成功・失敗と例外を値なし Result に引き継ぎます。
`WithoutErrorValue` は成功値と例外を `Result<T>` に引き継ぎます。
`ThrowIfError()` は失敗を例外へ戻す明示的な境界です。
保持している ResultException を送出するため、元の例外型そのものを直接 catch する用途とは異なります。

## 連結と変換

| 操作 | 意味 | コールバック |
| --- | --- | --- |
| Then | 成功時に次の Result を返す処理へ進む | `T → Result<U>` など |
| ThenError | 失敗時に回復・別の失敗への変換を行う | `ResultException → Result<T>` など |
| IfSuccess / IfError / IfElse | 該当する分岐で副作用を行い、元の結果を返す | 成功用・失敗用のアクション |
| Resolve | 成功・失敗の両方を処理して通常の値へ取り出す | 成功用・失敗用の関数 |

`Then` で成功値を変換する場合、変換した値を `AsResult()` または `CreateSuccess` で包みます。
失敗結果は変換先の Result へ引き継ぎます。

```csharp
var result = ParseCount(text)
    .Then(count => (count, label: "items").AsResult())
    .Then(pair => Validate(pair.count))
    .ThenError(error => Recover(error));
```

## 非同期版

`Task` には `ThenAsync`、`ValueTask` には `ThenValueAsync` を使います。
同期 Result から直接非同期コールバックにつなげられます。
非同期の受け手を渡した場合は、同期コールバックでの値変換もチェーンにできます。

```csharp
return await input.ThenAsync(value => Serialize(value, ""))
    .ThenAsync(text => (stream, text).AsResult());
```

非同期の受け手は通常の `await` の規則で待機します。
`.ConfigureAwait(false).ThenAsync(...)` とすると、次のコールバック前の待機でコンテキストへの復帰を省けます。
生成コードはコールバックを呼んだ後、その戻り値を `ConfigureAwait(false)` で待機します。
コールバック内部の待機には、その実装自身の指定が適用されます。

## Result と例外の境界

**Result を返す処理の契約を信頼し、呼び出し側でその例外を勝手に Result 化しません。**
例外を Result 化する必要がある場合は、その処理自身の責務として実装します。
Result を返すメソッドであることは、例外が絶対に送出されないという保証ではありません。

**Result を使わない外部システムとの接続点では、その接続を担当する実装が例外を Result 化します。**
たとえば TextSerializer は Reader / Writer の操作を捕捉しますが、派生側の文字列版
Serialize / Deserialize の例外は捕捉しません。FileSystem も Serializer や
Result を返す拡張点の例外を Result 化せず、担当するファイル操作の例外を変換します。

Result の連結・分岐とその非同期版は、コールバックの例外を捕捉しません。
非同期版では元の Task の例外やキャンセルもそのまま伝播します。
OperationCanceledException を Result に格納する場合と、Task がキャンセルされる場合は別の状態です。
ThenError は失敗 Result に反応し、投げられた例外やキャンセル済み Task を回復するものではありません。

```csharp
// Result を使わない API の操作だけを捕捉する。
try
{
    return (await reader.ReadToEndAsync().ConfigureAwait(false)).AsResult();
}
catch (Exception exception)
{
    return exception;
}
```

これは catch 自体を禁止する規則ではありません。
例外伝播時の資源解放や、複数の破棄失敗を集約して再送出する処理は必要です。
元の例外と清掃失敗の両方を保持し、清掃だけのために通常の失敗 Result へ変換しないようにします。
Interaction が未処理例外を専用 Port に委譲する処理も、明示的なフロー境界として別の責務です。

## Scheduler の明示的な結果変換境界

Core の `CancellationScheduler.TimeoutAsync` は、期限付き処理を実行する利用側が明示的に選ぶ境界です。
この API はユーザー指定の契約として、Cancel の例外と対象 Task の送出例外・キャンセル完了を Result 化し、
処理が返した失敗 Result も含めて集約します。
詳細は [CancellationScheduler](./CANCELLATION_SCHEDULER.md) を参照してください。

## 実装・検証時の確認

- 失敗時に、後続処理や副作用を実行していないか。
- 返された失敗と、投げられた例外・Task のキャンセルを混同していないか。
- Result を返す委譲先まで、一括の try / catch に含めていないか。
- 外部操作の失敗を担当する実装内で Result 化できているか。
- 例外が伝播しても資源を解放し、既存データを不必要に破壊しないか。

型定義と拡張は [Core の ResultAPI](../InteractionFlow.Core/ResultAPI/Entities) を参照してください。
形式変換固有の契約は [SerializationSystem](./SERIALIZATION.md)、
ファイル保存と清掃の契約は [FileSystem](./FILESYSTEM.md) にあります。
