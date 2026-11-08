# Analyzer と Context 引数ジェネレーターの計画

記録日: 2026-11-08

この文書は、サンプルによる API 検証を踏まえた合意と、今後決める事項を記録します。
以下は追加機能の計画であり、属性・診断・生成機能はまだ実装していません。
属性名と生成コードは構想を示すものです。

関連: [現在の Analyzer](../../InteractionFlow.Analyzers/README.md)、
[Context 値の現行契約](../CONTEXT_VALUES.md)、
[従来の検討記録](./CONTEXT_AND_REACTION_RESULTS.md)

## 基本方針

- 現在のレイヤー間依存方向チェックと依存ノード宣言チェックを維持する。
- Context の保証は `GetRequired<T>()` による動的検証を基本とする。
- Analyzer は静的に判断しやすい構文から対応し、完全な実行時保証は目指さない。
- 実装ごとに異なる Context を要求できる自由度を維持する。
  Port 全体へ、すべての実装の要求を必須条件として押し上げない。
- 解析不能と、解析できた構成での不足を区別する。検査できない経路を保証済みとしない。

## 追加する誤用検査

### using / Dispose の漏れ

利用側が取得したスコープの解放漏れを検査する。
初期対象は `Bind`、`GetScheduler`、`AddCancelableTask` の戻り値を想定する。
DI が所有する Port 本体と、利用側が所有する接続・登録スコープを混同しない。

`using` / `using var` と、例外経路でも解放する `try/finally` を扱う方向とする。
戻り値やフィールドへの所有権移譲、別メソッドでの Dispose などの詳細な対応範囲は実装前に決める。
最初から任意の IDisposable の全所有権をメソッド間で追跡する検査にはしない。

### Interaction の直接ネスト

Interaction の実行処理から別の Interaction の `ExecuteAsync` を直接呼ぶ構成に警告する。
相互作用の順序・分岐・反復は SystemFlow で合成する方針を示す。

SystemFlow からの Interaction 実行と、SystemFlow 同士の合成は対象外とする。
初期対象はメソッドシンボルを特定できる直接呼び出しとする。
ヘルパーやデリゲートを経由する間接呼び出しの完全検出は初版の目標にしない。

両検査の診断 ID、設定方法、具体的な診断条件、コード修正支援は未決定。

## Context 引数ジェネレーター

任意の型・個数の引数を取るメソッドへ属性を付け、
`IFlowContext` から `GetRequired<T>()` で引数を取得する入口を生成する。
ここでいう任意引数は、C# の省略可能引数だけを意味するものではない。

```csharp
// 利用側の宣言。生成を指示する属性名は未決定。
[ContextArguments]
private Task<ReactionEnd> TalkAsync(SampleMode mode, SessionState session)
{
    // 型付きの引数で処理を書く。
}
```

生成される入口には、要求する型の属性も付ける。

```csharp
[RequiredContext<SampleMode>, RequiredContext<SessionState>]
private Task<ReactionEnd> TalkAsync(IFlowContext context)
{
    return TalkAsync(
        context.GetRequired<SampleMode>(),
        context.GetRequired<SessionState>());
}
```

これにより、本体の記述を簡潔にし、要求する値を引数とメタデータで明示する。
生成入口にも動的検証が残り、提供側の不足が生成だけで保証されるわけではない。

`RequiredContext<T>` はそのメソッド・実装の要求を表す。
実装メソッドの属性はインターフェイスの契約へ自動的に引き継がれるものとしない。
別アセンブリのメソッドもメタデータから要求を取得できる形を想定する。

## 構成を明示する属性

`InteractionFlowBuildInfo` が付いた静的メソッドを、一つの有効な構成を解析する入口とする。
本番用・デバッグ用などを別の構築関数に分け、それぞれ独立した構成として検査する。
関数名を構成のラベルとして診断に表示する。

```csharp
[InteractionFlowBuildInfo]
private static async Task Main()
{
    var builder = new ScopeBuilder();
    builder.Apply(ConsoleProfile.Default)
        .UseFunction<IDoorOperation, ConsoleDoorOperation>()
        .UseFunction<IDoorReaction, ConsoleDoorReaction>()
        .UseInteraction<OperateDoor>();

    using var scope = builder.BuildScope();
    using var flow = scope.BuildSystemFlow<DoorSystemFlow, IFlowContext>();
    using var context = new ScopedFlowContext(new FlowContext())
        .With(new DoorState());
    await flow.ExecuteAsync(context);
}
```

実行時の条件を厳密に評価することは当面行わず、その構成内で Use される実体を前提に警告する。
本番用とデバッグ用の登録を、一つの全体構成として混ぜない。
診断は「この構成の静的なモデルでは確認できない」という意味であり、全実行経路の不足を断定しない。

次は実装時の具体化事項として残す。

- 表示ラベルは関数名としつつ、同名メソッドを混同しない内部識別。
- 同一メソッド内の複数 builder / scope / 実行先の対応付け。
- `Apply` した Profile や構築ヘルパーを解析する範囲。
- 条件分岐や上書きにより同じ Port に複数実装が現れる場合の扱い。
- 実行時条件を無視する解析に由来する警告の表現・抑制方法。

## 構成情報を使った DI 不足検査

構成ごとの登録・解決モデルを作り、依存する Port やサービスを解決できるか検査する。
登録された実装のコンストラクター依存も対象とする方向とする。

明示的な Use の有無だけで不足を判定せず、実際のコンテナーの解決規則に合わせる。
組み込み登録、自動解決、ファクトリーなどの対応範囲を定め、未知の経路は不足と区別する。
この登録・解決モデルを Context 検査にも利用する。

## Context 検査の範囲

静的検査は `RequiredContext` 属性付きメソッドの呼び出し箇所に限定する。
属性が、静的検査への明示的な参加宣言になる。
属性のない手書きの `GetRequired<T>()` は、当面は動的検証に任せる。

直接呼び出しでは呼び出し先の属性を読み、DI の Port 越しの呼び出しでは次の順で検査する。

1. その構成で選択される実装メソッドを特定する。
2. 実装メソッドの RequiredContext 属性から要求型を取得する。
3. 渡される Context が提供する型と照合する。

実装の選択と提供内容を解析できる範囲で診断する。
Context の探索・上書き・親へのフォールバックを、実際の契約と異なる単なる型の集合に置き換えない。
実行途中で `.With(...)` する値もあるため、構成全体に型が一度現れたことだけを提供保証にしない。

ヘルパー呼び出しから要求を自動伝播させることは、初版では前提にしない。
必要なラッパーにも属性を明示・生成する方針を候補とし、詳細は生成仕様と合わせて決める。
生成済みコードと宣言元で診断が二重にならないようにする。

## 責務の分担

| 機能 | 担当すること |
| --- | --- |
| ジェネレーター | 型付き引数から入口と要求属性を生成する |
| 構成解析 | 構成ごとの登録と、選択される実装を把握する |
| DI 検査 | 解析可能な依存をその構成で解決できるか確認する |
| Context 検査 | 属性付き呼び出しで要求と提供を照合する |
| GetRequired | 実行時に必須値を検証する |

ジェネレーターと Context Analyzer は、要求の明示とその利用という補完関係にする。
実装側の要求を取り出せても、提供経路の解析の難しさまで消えるわけではない。

## 実装順と解析コスト

次の順序で段階的に進める。

1. スコープの破棄漏れ検査。
2. Interaction の直接ネスト警告。
3. 最小の Context 引数ジェネレーターと要求属性。
4. 構成ごとの登録・解決モデルと DI 不足検査。
5. そのモデルを使う、対応構文を限定した Context 検査。

Context 検査に進む前に、ジェネレーターと動的検証だけで十分かを再評価する。

属性の取得や呼び出し検出の負荷は比較的小さく、主な難所は構成と Context の追跡である。
構成ごとの登録表・解決情報とメソッドの要求をキャッシュし、呼び出しごとに構築処理を解析し直さない。
初期対象は明示的な Use、解析可能な Apply、ローカルな ScopedFlowContext.With、直接呼び出しを想定する。
未知のヘルパー、フィールド、複雑な分岐、再帰を無制限に横断せず、対応範囲を明示して打ち切る。

テストでは正例・誤用に加え、未解析経路を誤って不足または保証済みと判定しないことを検証する。
同じ構成を多数の呼び出しが利用するケースで解析時間も計測し、範囲を広げる判断に使う。

## ジェネレーターで未決定の事項

- 生成指示属性の正式名、属性の配置、生成先の partial 型などの宣言規則。
- 全引数を必須扱いから始めるか、任意値・既定値をどう明示するか。
- nullable と「Context に不在でもよい」を区別する方法。
- 同じ型の複数引数、ref / out、ジェネリックメソッドなどの対応範囲。
- オーバーロード衝突の診断と、不足例外で元の宣言位置を示す方法。
- 生成器のプロジェクト・配布形態と、Roslyn 参照の更新範囲。

これらは提案段階であり、この記録によって実装方式や完全な静的保証まで確定したものではない。
