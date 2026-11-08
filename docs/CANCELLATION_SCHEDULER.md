# CancellationScheduler

`InteractionFlow.Standard.Entities.Contexts` の `CancellationScheduler` は、期限までに処理が完了しなければ
`CancellationObject.Cancel()` を呼び、通知後も対象 Task の終了を待つ補助 API です。
CancellationObject 本体は変更せず、拡張メソッド `GetScheduler()` で取得します。

```csharp
using InteractionFlow.Core.Entities;
using InteractionFlow.Standard.Entities.Contexts;

using var scheduler = context.Cancellation.GetScheduler();
var result = await scheduler.TimeoutAsync(
    TimeSpan.FromSeconds(10),
    RunAsync());

async Task<Result> RunAsync()
{
    await SomeOperationAsync(context.Cancellation.GetToken());
    return Result.Success;
}
```

Task を受け取るため、処理は呼び出し側で開始します。非同期処理を Task.Run で包む必要はありません。
ラムダで同期的に Task を生成する場合、その生成前に投げられた例外はこの API の対象外です。

## 実行と結果

- 処理が先に完了すれば、期限の予約を解除し、処理の Result を返します。
- 期限が先なら Cancel を呼びます。通知が失敗しても対象 Task の終了を待ちます。
- 判定時点で対象 Task がすでに完了していれば、タイマーも完了していても処理の完了を優先します。
- Cancel の送出例外、Task の送出例外・キャンセル完了は、この明示的な境界で失敗 Result にします。
- 通知が成功して Task が失敗 Result を返した場合、その Result をそのまま返し、再ラップしません。
- 両方の失敗は AggregateException に保持します。通知失敗と処理側の失敗を保持し、
  Task 自体が複数例外で失敗している場合も全例外を残します。
- 期限到達自体は TimeoutException にしません。通知が成功し、処理が成功 Result で終了すれば成功です。

これは実行時間の上限ではなく、キャンセル要求を出す期限です。
Task がキャンセルに応じなければ、その終了まで戻りません。
Task は対象の CancellationObject のトークンを使うなど、通知へ協調して応答する必要があります。

## 寿命と利用制約

- 一つの Scheduler で同時に実行できる TimeoutAsync は一件です。完了後は再利用できます。
- 別々の Scheduler の寿命は独立しています。ただし同じ CancellationObject を使えば通知先は共有します。
- Dispose は予約を解除します。対象 Task をキャンセルしたり、処理の完了を待ったりしません。
  予約を解除しても TimeoutAsync は対象 Task の終了を待ち、その結果を返します。
- Dispose は冪等です。破棄後に新たな TimeoutAsync を開始することはできません。
- Dispose と通知開始は同期し、Dispose 完了後にその Scheduler から新たな Cancel を開始しません。
  すでに始まった Cancel は完了まで待ちます。
- Cancel の同期コールバック自身からの Dispose は、自己待機を避けて戻ります。
  その通知の完了と例外は TimeoutAsync が引き続き待機・取得します。
- コールバックから別スレッド上の Dispose を同期的に待つ使い方は避けてください。
  また、Cancel コールバック内で TimeoutAsync の完了を同期的に待つと、循環待機になります。

Dispose のために、本体の CancellationObject を破棄・リセット・キャンセルすることはありません。
WaitAndResetAsync を呼ばず、対象 Task や自身の Task をキャンセル完了の待機一覧へ登録しません。
Interaction のネストに由来する待機範囲の問題を解決する API ではありません。

予約は CancellationObject の期間へ結び付けません。予約が有効なまま外部でリセットすると、
期限時点の新しいキャンセル元へ通知する可能性があります。using で予約の有効期間を管理してください。

## 引数

時間はゼロ以上・Int32.MaxValue ミリ秒以下、または `Timeout.InfiniteTimeSpan` を受け付けます。
ゼロは未完了 Task に対して待機なしで通知を試み、無期限は Task の完了だけを待ちます。
引数不正、null、破棄済み、同時利用は同期的に例外を送出します。

## 検証

`InteractionFlow.Standard.Tests/Contexts/CancellationSchedulerTests.cs` で、完了優先、通知後の待機、
失敗 Result・例外・キャンセル完了の保持、複数例外、予約解除、並行破棄、コールバック内の破棄を検証します。

関連: [Result API](./RESULT.md)、[寿命管理の検討記録](./design-notes/CANCELLATION_LIFETIMES.md)
