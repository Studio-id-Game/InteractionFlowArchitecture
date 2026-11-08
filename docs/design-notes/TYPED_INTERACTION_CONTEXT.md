# Interaction の Context 型を引き継ぐ契約

記録日: 2026-11-09

Raylib サンプルの API 検証で見つかった、Interaction の Context 引数に関する設計上の制約を記録します。
型付き Interaction を実装しました。この文書は、発見の経緯と採用した型・実行契約を記録します。

関連: [サンプルの整合性分析](../../InteractionFlow.Samples.Raylib/ARCHITECTURE_ANALYSIS.md)、
[Context 引数ジェネレーター計画](./ANALYZER_AND_CONTEXT_GENERATOR_PLAN.md)

## 発見の経緯

`CounterFlowContext` は入力・状態遷移・表示取得のアクセス契約を提供します。
これらを用途別の interface 型のプロパティとして公開すれば、提供する値を型の宣言から読み取れます。
TryGetCore もプロパティを返す形で整理できます。

さらに Port の引数を CounterFlowContext にできれば、プロパティへ直接アクセスでき、
値の探索と必須値の実行時取得を省けます。しかし、現在の Interaction の実行契約がその接続の障壁になります。
プロパティ公開と Port の型変更も、この記録時点では検討案です。

## 導入前の契約の非対称性

| 要素 | Context 型の扱い |
| --- | --- |
| Port | 利用側がメソッドの引数を自由に定義できる |
| SystemFlow | `SystemFlow<TContext>` で要求する Context 型を指定できる |
| Interaction | `ExecuteAsync(IFlowContext)` / `ExecuteCoreAsync(IFlowContext)` に固定されていた |

型付き SystemFlow から Interaction へ渡す際、静的な Context 型が IFlowContext へ落ちます。
Port が専用 Context を要求すると、Interaction 内で実行時の型確認・変換が必要です。
ExecuteCoreAsync の引数だけを派生 Context 型に置き換えて override することはできません。

このため、単に Port を型付きにするだけでは、SystemFlow から Port まで型の要件を一貫して表現できません。
サンプル固有のキャストで吸収し続けるより、基盤 API の表現力として検討する価値がある問題です。

## 採用した契約

実行に必要な Context 型と、共通の依存ノードとしてのメタデータを分けます。

| 型 | 責務 |
| --- | --- |
| `IInteractionNode` | `IFlowNode` を継承し、Interaction の Layer / FunctionTypes を提供する |
| `IInteraction<in TContext>` | 指定した Context 型での `ExecuteAsync(TContext)` と終了結果の生成契約 |
| `IInteraction` | `IInteraction<IFlowContext>` を継承し、従来の汎用実行契約を維持する |
| `Interaction<TContext>` | 型付きの本体・キャンセルフックと、共通の例外・キャンセル処理 |
| `Interaction` | `Interaction<IFlowContext>` と `IInteraction` による従来の派生実装用の基底 |

```text
SystemFlow<CounterFlowContext>
  → Interaction<CounterFlowContext>
    → CounterFlowContext を要求する Port
```

型付き派生クラスでは `ExecuteCoreAsync(TContext)` と `OnCancellation(TContext)` を実装できます。
キャストせず、受け取った Context を専用の Port へ渡せます。
例外・キャンセルの処理 Port は従来どおり `IFlowContext` を受け取り、同じインスタンスを処理します。

専用 Context を要求する `Interaction<TContext>` は、非ジェネリックの `IInteraction` を実装しません。
`IInteractionNode` として依存グラフへ登録できても、任意の `IFlowContext` で実行できることは意味しません。

`IInteraction<TContext>` は反変です。任意の `IFlowContext` で動作する Interaction を、
専用 Context の呼び出し先として利用することはできます。その逆の代入はできません。

```csharp
IInteraction<IFlowContext> general = generalInteraction;
IInteraction<SpecializedContext> specialized = general;
// IInteraction<SpecializedContext> を IInteraction<IFlowContext> へ広げることはできません。
```

`IInteraction` 自体をメタデータ専用に変える案は採用しませんでした。
既存の `IInteraction` 変数から `ExecuteAsync(IFlowContext)` を呼ぶソースコードと、
直接実装した型から `IInteraction.GetEnd` を利用する生成契約を維持するためです。
旧 `IInteraction.ExecuteAsync` だけを明示実装した型も扱えるように、
`IInteraction` がジェネリック側の実行を従来のメソッドへ委譲する既定実装を持ちます。

## 登録と既存実装

`UseInteraction<TImplementation>()` の型制約を `IInteractionNode` にしました。
型付き・非ジェネリックのどちらも、従来と同じ一つの型引数で登録できます。
依存ノードの列挙と Layer / FunctionTypes は、両方の実装で同じ契約を使います。

既存の `Interaction` 派生型は、引き続き `ExecuteCoreAsync(IFlowContext)` と
`OnCancellation(IFlowContext)` を使えます。処理本体の登録・解除、キャンセル通知 Task、
後始末の順序、待機・リセットと失敗保持の実装は `Interaction<TContext>` に一度だけ置き、
汎用版も同じ実装を通ります。

Analyzer は名前空間の境界と `IDependencyNode` への実装関係で判断するため、
ジェネリック型の追加に伴う特別な型名判定や、新しい診断は追加していません。

## 引き続き区別すること

- `new FlowContext(parent)` は、親と同じ専用 Context 型になるものではない。
  型付き Interaction を呼ぶ側は、その Interaction が要求する型を渡す。
- 型付きプロパティと、With による追加値の上書き・TryGetCore の探索契約との違い。
- Context の破棄後にプロパティから値を取得できるかと、取得済み値の寿命の区別。

追加値の上書きに関する別の検討は、
[Context 型ごとの追加値バリデーター](./CONTEXT_ADDITIONAL_VALUE_VALIDATION.md) に記録しています。

型付き契約を導入しても、Interaction が別の Interaction を直接実行する構成へは変更しません。
合成・進行・終了判断は引き続き SystemFlow の責務です。
Context も実行エンジンにはせず、値へのアクセスを管理する役割を維持します。
