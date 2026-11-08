# 文脈値の取得

必須の文脈値には Core の `IFlowContext` 向け拡張メソッド `GetRequired<T>()` を使います。
`InteractionFlow.Core.Entities.Contexts` 名前空間をインポートしてください。

```csharp
var mode = context.GetRequired<SampleMode>();
var greeting = context.TryGet<ParrotGreeting>(out var custom)
    ? custom.Text : ParrotRules.DefaultGreeting;
```

- 必須値は `GetRequired<T>()`、不在を許す任意値は `TryGet<T>()` で取得します。
- 必須値が不在（`TryGet` が false）なら `RequiredContextValueNotFoundException` を送出します。
  `RequiredType` から要求型を取得でき、基底型は `InvalidOperationException` です。
- `GetRequired` の呼び出し元は Caller 属性で自動取得します。例外の `MemberName`、`FilePath`、
  `LineNumber` に保持し、メッセージにはファイル名・行番号・メンバー名を表示します。
  フルパスはメッセージには含めず、`FilePath` から参照できます。
- `TryGet` 自体の例外は変換しません。探索中の失敗や破棄済みスコープと、値の不在を区別できます。
- 探索・上書き・親へのフォールバック・Entry の解決は既存の `TryGet` に委ねます。
  値の所有権や寿命、キャンセルオブジェクトは変更しません。
- null の context は `ArgumentNullException` になります。

取得結果はその時点の値です。呼び出し元による提供や、その後の寿命まで保証する API ではありません。
不足例外は既存の Interaction の例外処理へ伝播します。
Reaction が終了結果として例外を返す契約なら、その境界で専用例外を結果に変換します。

必須値の要求は `GetRequired<T>()` に統一します。
将来の Analyzer はこのメソッドのシンボルを解析の入口にできますが、今回は解析機能を追加しません。
動的検証を基本とし、静的に検証しやすい構文への補助を今後検討します。

## FlowContext の構成と寿命

```csharp
using var root = new FlowContext().With(new SessionState());
using var child = new FlowContext(root).With(new SampleMode());
```

親なしはルート、親ありは独立した追加値を持つ子です。子は親の CancellationObject を共有します。
追加値を新しい順に探索し、その後 TryGetCore へ委譲します。
既定の TryGetCore は親を探索し、ルートでは Cancellation を提供します。
親スコープの上書きも維持します。値の追加は Cancellation プロパティの参照先を変更しません。

派生型の public override TryGet は protected override TryGetCore へ移行します。
独自値がなければ base.TryGetCore を呼びます。追加値は派生型の値にも優先されます。

公開の With / TryGet / Dispose は非 virtual で、破棄後の取得・追加を共通で拒否します。
DisposeCore は基底の管理状態を解放した後に一度だけ呼び、例外もそのまま伝播します。
親・追加した値や Entry・Cancellation は破棄せず、Cancellation は破棄後も参照可能です。
派生型の独自プロパティのアクセスや、並行利用の安全性まで保証するものではありません。

旧 ScopedFlowContext は削除しました。new ScopedFlowContext(parent) は new FlowContext(parent) へ、
ルートを包んでいた new ScopedFlowContext(new FlowContext()) は new FlowContext() へ変更してください。
