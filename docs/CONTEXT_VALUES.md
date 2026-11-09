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

## 探索時の割り当て

FlowContext の追加値探索では、通常の型不一致や null を未発見として扱い、
その候補ごとの例外・失敗 Result は生成しません。通常値の直接取得では循環検出用の集合も生成しません。
Entry は反復処理で最大100回まで展開し、正常なネストでも循環検出用の集合は生成しません。
利用者が渡した Entry を数え、With 内部の Box は数えません。100個目の Entry の保持値までは解決し、
さらに Entry の展開が必要なら失敗を確定します。これは100段を超える正常なチェーンも拒否する制約です。
失敗時だけ起点から最大100回の追加診断を行い、参照同一性で循環を調べます。
`EntryResolutionLimitException` に要求型・展開上限・循環検出結果・診断した展開数を保持します。
循環未検出は診断範囲内で見つからなかったという意味で、チェーン全体の非循環を保証しません。
追加診断は再探索時点の情報です。探索中の RefEntry の並行変更はサポートせず、診断結果で最初の失敗を取り消しません。
Context ではこの例外を ResultException に保持して伝播し、別候補へフォールバックしません。
公開の `Entry.Parse<T>()` は、未発見の理由を含む失敗 Result を引き続き返します。
探索順序・親への委譲・RefEntry の更新反映は維持し、取得結果のキャッシュは導入していません。

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

## Context 型ごとの追加値の検証

`FlowContext.With<T>(T value)` は、次の順序で追加値を扱います。

1. 破棄済みでないこと、値が null でないことを共通入口で確認する。
2. そのインスタンスの `ValidateAdditionalValue<T>(T value)` を呼ぶ。
3. 検証が成功した場合だけ値を登録する。

`With` は非 virtual のままです。派生型は protected virtual の
`ValidateAdditionalValue` を override し、その Context 型固有の制約を定義できます。
既定の実装は何も拒否せず、既存の追加・上書き・Entry の探索動作を維持します。

```csharp
protected override void ValidateAdditionalValue<T>(T value)
{
    base.ValidateAdditionalValue(value);
    if (value is IReservedAccess)
        throw new ArgumentException("固定されたアクセス契約は上書きできません。", nameof(value));
}
```

`T` は `With` に指定された型で、検証には元の値を渡します。
検証が送出した例外はそのまま伝播し、`With` は対象値を登録せず、既存の追加値も変更しません。
バリデーター自身は検証だけを行い、Context の登録状態を変更しないでください。
破棄済み・null の場合はバリデーターを呼びません。両方に該当する場合は破棄済みの確認が先です。

検証は追加先のインスタンスに限ります。`new FlowContext(specializedContext).With(replacement)` は
通常の子 Context の規則に従い、親 Context の追加制約を呼び出したり継承したりしません。
子が親の値を上書きできる探索順序も維持します。C# の型継承による override の継承とは別です。

登録時の型・実際の型・interface・Entry をどこまで調べるかは派生型の責任です。
Core は汎用的な上書き禁止規則や、追加後に変更された RefEntry の値まで保証しません。
設計の背景は [追加値バリデーター](./design-notes/CONTEXT_ADDITIONAL_VALUE_VALIDATION.md) を参照してください。
