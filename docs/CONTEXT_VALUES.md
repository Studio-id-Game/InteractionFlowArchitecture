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
