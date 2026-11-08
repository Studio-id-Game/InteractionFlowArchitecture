# Ctrl+C 接続とキャンセル予約の寿命管理案

記録日: 2026-11-07

Parrot の新 Core / Standard 移行を振り返った議論の記録です。
当初の検討案と、その後の採用結果を記録します。未採用の API 名は仮称です。
現行契約は [Standard Console](../CONSOLE.md) を参照してください。

## 2026-11-08 の決定と実装

案1は採用し、Bind は private なスコープクラスを IDisposable として返す API へ変更しました。
同じ Port インスタンス・同じ CancellationObject の参照カウントを管理し、最後の Dispose で解除します。
通常の Unbind は廃止し、強制解除は ForceResetMemoryState または本体の Dispose とします。
リセット前の古いスコープが再接続後の参照を解放しないよう、接続ごとの識別を持ちます。
異なる対象・別 Port インスタンスの同時接続は引き続き例外です。

NestedExecuteAsync の両オーバーロードは Obsolete で旧 API としてマークしました。
SystemFlow で相互作用を合成する方針とし、実装は互換性のため残します。
同じ CancellationObject を共有した Interaction 同士の直接ネストは循環待機を起こし得るため、
単純な ExecuteAsync 呼び出しへの置換は推奨しません。

案2は後述の再検討により、Task を受け取る CancellationScheduler として実装しました。

### 案2の再検討結果（2026-11-08）

Core.Entities.Contexts の GetScheduler 拡張メソッドが、IDisposable な CancellationScheduler を返します。
TimeoutAsync(TimeSpan timeoutTime, Task<Result> task) は処理と期限を競争させ、期限時に Cancel を呼び、
その後も task の終了を待ちます。通知側と処理側の例外を Result へまとめます。
先の「Dispose で例外を再送出する」案に代わり、結果の取得を await で明示する形を採用しました。

一件ずつの利用・完了後の再利用を許し、Dispose は予約解除と開始済み通知との同期だけを担当します。
当初は Standard に配置し、世代同期を持たせませんでした。
その後 Core へ移設し、各予約を開始時の状態へ結び付けました。
リセット後の古い予約は通知を省略し、次の状態へ影響しません。
世代操作は内部 API のみに閉じています。
契約と使用例は [CancellationScheduler](../CANCELLATION_SCHEDULER.md) を参照してください。

以下は決定前の検討記録です。
現行の接続契約は [Standard Console](../CONSOLE.md#ctrlc-の接続) を参照してください。

## CancellationObject の登録・待機契約の更新（2026-11-08）

Task 登録も IDisposable のスコープへ変更し、正常終了時に登録解除できるようにしました。
待機中はキャンセル状態を維持し、全対象の終了後にリセットします。待機中の新規登録・重複待機は拒否します。
旧 WaitAndResetAsync は Obsolete の転送 API とし、WaitForTasksAndResetCancellationAsync を使用します。
即時・遅延完了で失敗集約を共通化し、通知中 Source の破棄は通知終了まで遅延します。
現在の契約は [CancellationObject](../CANCELLATION.md) を参照してください。

## 問題の整理

Parrot では、Console Port を保持する DI Scope の寿命より、Ctrl+C の接続期間や
時間制限付き会話のキャンセル予約の有効期間が短くなります。
利用側が、それぞれを実行範囲の終了時に閉じる必要があります。

ただし、この二つは同じ種類の問題ではありません。
Ctrl+C の接続管理は ConsoleSystem API の利用手順に伴う負担です。
タイマーの破棄は一般的な非同期処理の責任であり、InteractionFlow 固有の欠陥ではありません。
後者にも、意図を明確にし、正しい実装をまとめる補助 API を提供する余地があります。

## 案1: Bind が接続スコープを返す

現状の利用例は、[SelectSample](../../InteractionFlow.Samples.Parrot/Interactions/SelectSample.cs) と
[TalkToParrot](../../InteractionFlow.Samples.Parrot/Interactions/TalkToParrot.cs) にあります。

```csharp
binding.Bind(context);
try
{
    // 入力・表示
}
finally
{
    binding.Unbind();
}
```

Bind の戻り値を IDisposable なスコープにして、次のように書ける案です。

```csharp
using var bindingScope = binding.Bind(context);
```

専用の sealed class BindScope を候補とします。
低頻度の接続操作なので、割り当て削減より実装の明快さを優先します。
struct の値コピーによる複数 Dispose の問題を避け、同じインスタンスの Dispose は冪等にします。

### 再 Bind の選択肢

| 案 | 同じ対象への再 Bind | 利点と制約 |
| --- | --- | --- |
| 所有者だけ解除 | 最初に接続した Scope だけが解除し、後続は解除しない | 現行の再 Bind が何もしない契約に近い。ただし後続 Scope の生存は接続を保証しない |
| 参照カウント | 取得ごとにカウントし、最後の Dispose で解除 | 部品ごとに接続期間を確保できる。管理状態と契約が増える |
| 再 Bind 拒否 | 同じ対象でも例外 | 所有者が明快。接続を確保する部品同士のネストには調整が必要 |

Parrot のように Interaction が順次実行される構成なら、再 Bind 拒否はほぼ妨げになりません。
ただし、単独実行できる親子の Interaction が双方で Bind する構成では衝突します。
接続を親またはラッパーへ集約するか、同じ対象には参照カウントを許すかを判断する必要があります。
「再 Bind 拒否を汎用 API の第一候補とする」とまでは、現時点で決めていません。

NestedExecuteAsync は独立した CancellationObject を作ります。
この場合は異なる対象への接続競合なので、同じ対象の再 Bind を許可するだけでは解決しません。
Ctrl+C を親へ接続して子へ伝播する構成など、接続対象の選択を別に設計する必要があります。

### 実装時に決めること

- Scope は自分が取得した接続だけを解除する。古い Scope の Dispose が新しい接続を解除しないようにする。
- 手動 Unbind を残すか、通常の解除を返却 Scope の Dispose へ統一するかを決める。
- DI Scope 破棄、ForceResetMemoryState、再接続と返却 Scope の関係を決める。
- 発火中の通知と解除の同期、通知コールバックからの解除について現行契約を確認する。

## 案2: CancellationObject のキャンセル予約をスコープ化する

[TalkToParrot](../../InteractionFlow.Samples.Parrot/Interactions/TalkToParrot.cs) では、
CancellationTokenSource と Register の戻り値を using で保持し、10秒後の Cancel を予約しています。
予約が残ると、会話を抜けた後に同じ CancellationObject を使うメニューへ影響します。

この定型処理を、例えば次の API にまとめる案です。

```csharp
using var scheduledCancellation =
    context.Cancellation.ScheduleCancellation(TimeSpan.FromSeconds(10));
```

返却スコープがタイマーと登録を保持し、Dispose で予約を解除します。
「現在の実行範囲だけ有効なキャンセル予約」という意図を直接表現できます。

### 提供すると価値のある保証

- Dispose を冪等にする。
- 解除とタイマー発火の競合を扱い、Dispose 完了後にその予約から新たな Cancel 呼び出しが発生しないようにする。
- すでに実施されたキャンセル要求を Dispose で取り消すものではない。
- キャンセルコールバック内での Dispose の可否や、待機によるデッドロックを含む再入条件を明示する。
- 予約の引数検証、発火時の例外の扱い、複数予約の独立性を定義する。

通常の IDisposable API と同じく、利用側は using または Dispose で寿命を示します。
破棄漏れを静的に防げないことはこの案固有の弱点ではなく、採用を妨げる理由にしません。
静的な利用規約検査が必要なら Analyzer が別途候補になります。
コールバック形式で内部が寿命を管理する方式もありますが、今回は書き方を制約する根拠がありません。

### 期間への結び付けは追加検討

予約をキャンセル状態の現在の世代に結び付け、WaitAndResetAsync 後に古い予約を失効させる案もあります。
ただし正常終了ではリセットされないため、それだけで「会話終了後の予約」をすべて防げません。
実行範囲の終了まで自動認識するには、Interaction の寿命との新たな契約が必要です。
最初は返却スコープ付き予約に留め、世代管理は必要性を別に判断するのが妥当と考えます。

CancellationObject 本体のメソッドとするか拡張メソッドとするかは、保証に内部状態へのアクセスが必要かで判断します。

## 現時点の結論

両案とも、責任を曖昧にせず、標準的な using 一つで寿命を表せることが主な価値です。
Bind の再入方針と予約解除の競合保証は未決定です。実装前に契約として明文化します。

関連: [Context 契約保証と ReactionEnd / Result 統合の検討](./CONTEXT_AND_REACTION_RESULTS.md)
