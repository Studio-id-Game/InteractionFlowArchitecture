# CancellationScheduler

`InteractionFlow.Core.Entities.Contexts` の `CancellationScheduler` は、期限までに処理が完了しなければ
`CancellationObject.Cancel()` を呼び、通知後も対象 Task の終了を待つ補助 API です。
Core の拡張メソッド `GetScheduler()` で取得します。
CancellationObject の世代への結び付けは Core 内部で行い、世代操作の公開 API は追加しません。

```csharp
using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;

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

## 値を返す処理

`TimeoutAsync<T>(TimeSpan timeoutTime, Task<Result<T>> task)` は、通知が成功した場合に対象の Result<T> をそのまま返します。

```csharp
var result = await scheduler.TimeoutAsync(TimeSpan.FromSeconds(10), LoadValueAsync());
if (result.Try(out var value, out var error))
{
    // 成功値を利用
}
```

値なし版と、期限判定・通知・全処理の待機・例外集約・破棄の契約は共通です。
通知失敗時は、対象が成功していても失敗 Result<T> を返し、成功値は返しません。
対象の失敗 ResultException は通知失敗がなければ再ラップしません。
T の内容は解釈しません。例えば成功値に ReactionEnd を入れても、その内部の失敗を自動集約しません。
Parrot は通知失敗を優先するため、RunAsync は ReactionEnd をそのまま成功値として返します。

```csharp
async Task<Result<ReactionEnd>> RunAsync()
{
    var end = await TalkAsync();
    return end.AsResult();
}
```

この選択では通知失敗がなければ、内部の成功・失敗を問わず元の ReactionEnd を返せます。
通知失敗時には内側の ReactionEnd は返しません。
両者の失敗を集約したい利用者は、RunAsync で内側の失敗を外側の Result<T> へ変換する方式も選べます。
両オーバーロードで同時利用枠と Dispose の状態を共有し、完了後は値の型や値なし版を切り替えて再利用できます。

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
WaitForTasksAndResetCancellationAsync を呼ばず、対象 Task や自身の Task をキャンセル完了の待機一覧へ登録しません。
Interaction のネストに由来する待機範囲の問題を解決する API ではありません。

## 予約とリセットの世代

各 TimeoutAsync の開始時に現在のキャンセル状態を捕捉します。GetScheduler の取得時ではありません。
同じ Scheduler を再利用する場合も、その時点の状態に予約を結び付けます。
リセットが完了した後に古い予約の期限が来ても、新しい状態には通知しません。
古い予約が無効になっても対象 Task は終了まで待ち、その結果は通常どおり返します。
リセットを自動実行したり、対象 Task を完了させたりする機能ではありません。

状態の同一性確認と通知中の参照確保は CancellationObject の同じロック内で行います。
先にリセットが完了すれば通知を省略し、先に通知対象を確保すればその古い状態だけへ通知します。
通知中にリセットされても、古い Source の破棄は通知終了まで延期します。
待機・リセット中（完了前）はまだ同じ状態であり、キャンセル要求も維持されています。

この保証は予約の寿命管理を不要にはしません。using で Scheduler の有効期間を管理してください。

## Core への移行

配置と名前空間を InteractionFlow.Standard.Entities.Contexts から InteractionFlow.Core.Entities.Contexts へ移しました。
旧名前空間の using を削除し、Core の名前空間を使用してください。
GetScheduler / TimeoutAsync / TimeoutAsync<T> の呼び出し形式は同じです。

## 引数

時間はゼロ以上・Int32.MaxValue ミリ秒以下、または `Timeout.InfiniteTimeSpan` を受け付けます。
ゼロは未完了 Task に対して待機なしで通知を試み、無期限は Task の完了だけを待ちます。
引数不正、null、破棄済み、同時利用は同期的に例外を送出します。

## 検証

`InteractionFlow.Standard.Tests/Contexts/CancellationSchedulerTests.cs` で、完了優先、通知後の待機、
失敗 Result・例外・キャンセル完了の保持、複数例外、予約解除、並行破棄、コールバック内の破棄を検証します。
CancellationSchedulerGenerationTests.cs では古い予約、新世代への再利用、通知中・別スレッドからのリセットを検証します。

関連: [Result API](./RESULT.md)、[寿命管理の検討記録](./design-notes/CANCELLATION_LIFETIMES.md)
