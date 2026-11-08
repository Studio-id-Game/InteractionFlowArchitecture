# Interaction の Context 型を引き継ぐ契約の検討

記録日: 2026-11-09

Raylib サンプルの API 検証で見つかった、Interaction の Context 引数に関する設計上の制約を記録します。
型付き Interaction は未実装です。この文書は検討記録であり、現行契約や採用済みの仕様を変更しません。

関連: [サンプルの整合性分析](../../InteractionFlow.Samples.Raylib/ARCHITECTURE_ANALYSIS.md)、
[Context 引数ジェネレーター計画](./ANALYZER_AND_CONTEXT_GENERATOR_PLAN.md)

## 発見の経緯

`CounterFlowContext` は入力・状態遷移・表示取得のアクセス契約を提供します。
これらを用途別の interface 型のプロパティとして公開すれば、提供する値を型の宣言から読み取れます。
TryGetCore もプロパティを返す形で整理できます。

さらに Port の引数を CounterFlowContext にできれば、プロパティへ直接アクセスでき、
値の探索と必須値の実行時取得を省けます。しかし、現在の Interaction の実行契約がその接続の障壁になります。
プロパティ公開と Port の型変更も、この記録時点では検討案です。

## 現行契約の非対称性

| 要素 | Context 型の扱い |
| --- | --- |
| Port | 利用側がメソッドの引数を自由に定義できる |
| SystemFlow | `SystemFlow<TContext>` で要求する Context 型を指定できる |
| Interaction | `ExecuteAsync(IFlowContext)` / `ExecuteCoreAsync(IFlowContext)` に固定されている |

型付き SystemFlow から Interaction へ渡す際、静的な Context 型が IFlowContext へ落ちます。
Port が専用 Context を要求すると、Interaction 内で実行時の型確認・変換が必要です。
ExecuteCoreAsync の引数だけを派生 Context 型に置き換えて override することはできません。

このため、単に Port を型付きにするだけでは、SystemFlow から Port まで型の要件を一貫して表現できません。
サンプル固有のキャストで吸収し続けるより、基盤 API の表現力として検討する価値がある問題です。

## 検討する方向

SystemFlow と同様に、次の契約を検討します。名称と具体的な継承関係は未決定です。

```text
IInteraction<TContext>
Interaction<TContext>

SystemFlow<CounterFlowContext>
  → Interaction<CounterFlowContext>
    → CounterFlowContext を要求する Port
```

既存の非ジェネリック版を IFlowContext 用の契約として残す方向も候補です。
汎用 Interaction と、専用 Context を要求する Interaction を併存させられることを目指します。

ただし、専用 Context を要求する Interaction を、任意の IFlowContext で実行可能なものとして扱ってはいけません。
共通の依存ノードとして扱う契約と、実行に必要な Context 型の契約を区別して設計する必要があります。

## 実装前に確認すること

- 型付き・非ジェネリックの Interaction の関係と既存コードへの影響。
- UseInteraction などの登録 API、依存ノードの列挙、Analyzer の対応。
- 基底 Interaction の例外・キャンセル処理、登録 Task の寿命を維持できること。
- SystemFlow 側で異なる Context 要件の Interaction を合成する際の型安全性。
- 終了用の `new FlowContext(parent)` など、専用 Context ではない子スコープの扱い。
- 型付きプロパティと、With による追加値の上書き・TryGetCore の探索契約との違い。
- Context の破棄後にプロパティから値を取得できるかと、取得済み値の寿命の区別。

追加値の上書きに関する別の検討は、
[Context 型ごとの追加値バリデーター](./CONTEXT_ADDITIONAL_VALUE_VALIDATION.md) に記録しています。

型付き契約を導入しても、Interaction が別の Interaction を直接実行する構成へは変更しません。
合成・進行・終了判断は引き続き SystemFlow の責務です。
Context も実行エンジンにはせず、値へのアクセスを管理する役割を維持します。
