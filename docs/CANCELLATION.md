# CancellationObject の要求・待機・リセット

`InteractionFlow.Core.Entities.Contexts.CancellationObject` は、キャンセル要求の通知と、
登録 Task の終了待機を分けて扱います。処理の強制停止や、体験の継続・終了判断は行いません。

## 登録はスコープで所有する

```csharp
var task = DoWorkAsync(cancellation.GetToken());
using var registration = cancellation.AddCancelableTask(task);
await task;
```

AddCancelableTask は private な登録スコープを IDisposable として返します。
処理の所有者は Task の結果を受け取り、正常終了・例外・キャンセルの各経路で登録を解除してください。
登録解除は Task の停止・待機・Dispose・例外報告を行いません。using 内での await は所有者の責務です。

- 同じ Task の複数登録は独立しています。Dispose は冪等です。
- リセットで無効化された古いスコープは、新しい登録へ影響しません。
- 完了 Task を件数に応じて自動削除する処理はありません。未報告の失敗を圧縮で消しません。
- null は ArgumentNullException です。

## 通知と待機の順序

```csharp
cancellation.Cancel();
var result = await cancellation.WaitForTasksAndResetCancellationAsync();
```

Cancel は現在のトークンへ同期的に通知します。登録 Task の終了は待ちません。
通知先コールバックの例外は Cancel の呼び出し元へ伝播します。

WaitForTasksAndResetCancellationAsync は以下の順に実行します。

1. キャンセル要求があることと、別の待機・リセットが進行中でないことを確認する。
2. 登録済み Task を待機対象として確定する。
3. 全対象が終了するまで待ち、Faulted Task の全例外を集約する。
4. 登録と古いキャンセル状態をリセットする。

待機中の IsCancellationRequested は true のままで、GetToken は同じキャンセル済みトークンを返します。
次の処理は待機・リセットの完了後に開始する、という逐次実行を基本とします。
待機中の AddCancelableTask は InvalidOperationException、重複した待機・リセットは失敗 Result です。
要求なしの待機・リセットも失敗 Result であり、登録は削除しません。

待機開始後に登録スコープを破棄しても、確定済みの Task は最後まで待ちます。
HasTask は待機中にはこの確定済み対象、通常時には現在の登録に未完了 Task があるかを返します。
登録外の処理や、Cancel の通知コールバック全体が完了したことを表すものではありません。

## 失敗の扱い

| Task の最終状態 | 待機結果への反映 |
| --- | --- |
| 正常完了 | 成功 |
| キャンセル完了（IsCanceled） | キャンセルによる停止として成功 |
| 例外完了（IsFaulted） | Task.Exception の全 InnerExceptions を AggregateException へ保持 |

OperationCanceledException を格納した Faulted Task も失敗です。
即時完了と遅延完了のどちらも同じ集約処理を使います。
一件の失敗で他の対象の待機を打ち切らず、失敗結果の場合も全対象の終了後に状態をリセットします。

## 通知中のキャンセル元の寿命

Cancel は内部ロック内で通知対象を確保し、ロックを外してコールバックを実行します。
通知中の Source は、待機・リセットが完了しても直ちには Dispose せず、最後の Cancel 呼び出しが
戻る時点まで破棄を遅延します。これにより通知途中の Source 破棄との競合を避けます。

待機 API が待つ対象は登録 Task です。通知コールバック全体の終了を追加で同期的に待つ API ではありません。
通知コールバックから、登録 Task のない即時リセットを呼んでも、通知自身の終了を待って停止しません。
ただし、コールバックが自分の完了を必要とする Task を同期的に待つ構成は避けてください。

## Interaction との連携

Interaction は本体を開始する前に完了通知 Task を登録し、登録スコープを Port の処理まで保持します。
本体が返した Task のキャンセルでは OnCancellation を待ち、その完了を確定してから CancellationPort へ進みます。
本体が Task を返す前の同期キャンセル例外と、開始前キャンセルでは OnCancellation を呼びません。

- 正常終了・通常例外でも、最後に登録を解除します。
- 後始末の失敗は通知 Task へ保持し、標準 CancellationHandling が待機結果として取得します。
- Port 自身が失敗したりリセットを省略したりして、その失敗を保持できない場合は、
  Interaction が元のキャンセル・後始末・Port の失敗を保持した例外を伝播します。
- 待機結果へ後始末の失敗を渡した後は、Port が成功へ回復した場合や別の失敗へ変換した場合も、その判断を尊重します。
- Port の結果・例外に元のキャンセルと後始末の失敗が既に含まれていれば、再集約しません。

OnCancellation 内で同じ対象の待機・リセットを待つことや、共有対象で親子 Interaction が互いを待つ構成は、
循環待機を作り得ます。この改修は Interaction ネストを可能にするものではありません。
体験を継続するか終了するかは、引き続き SystemFlow が決めます。

## Scheduler の世代への結び付け

Core の CancellationScheduler は各予約開始時の状態に限定して Cancel を通知します。
内部で状態の同一性確認と通知対象の確保を一体化し、リセット後の新しい状態へ古い予約が通知することを防ぎます。
通常の Cancel() は従来どおり現在の状態を対象とします。
Scheduler が無効な世代の通知を省略しても、その対象 Task の待機は続きます。

## 移行

- AddCancelableTask の返却スコープを保持し、必ず Dispose してください。
- WaitAndResetAsync は互換の転送メソッドとして残し、Obsolete を付けています。
  WaitForTasksAndResetCancellationAsync へ変更してください。
- 従来と異なり、状態のリセットは待機完了後です。待機中の新規登録は拒否します。

関連: [CancellationScheduler](./CANCELLATION_SCHEDULER.md)、[ライブラリ実装の詳細](./LIBRARY_IMPLEMENTATION_DETAIL.md)
