# InteractionFlow.Analyzers

Interaction Flow の実装規則を Roslyn Analyzer として検査するプロジェクトです。
現在は、レイヤー依存関係規則と `IDependencyNode` 宣言規則の 2 種類の診断を提供します。

## 診断 ID

| ID | 目的 | 既定の重大度 |
| --- | --- | --- |
| `InteractionFlowArchitecture001` | Interaction Flow のレイヤー間依存を検査する | Warning |
| `InteractionFlowArchitecture002` | `IDependencyNode` 実装クラスの依存ノード宣言を検査する | Warning |

実際の重大度は `.editorconfig` の `interactionflow_mode` と
`dotnet_diagnostic.<ID>.severity` で調整できます。

## Analyzer オプション

`.editorconfig` から以下のキーを読み取ります。

| キー | 値 | 動作 |
| --- | --- | --- |
| `interactionflow_enabled` | `True` / `False` | `True` の場合だけ診断します。未指定または不正値は無効扱いです。 |
| `interactionflow_mode` | `Error` / `Warning` / `Info` / `Hidden` など | Analyzer 内部で作る診断の重大度です。不正値は `Warning` になります。 |
| `interactionflow_allowed_roots` | カンマ区切り namespace root | 管理対象外 namespace への依存を許可する root です。`System` は常に含まれます。 |

リポジトリ直下の `.editorconfig` では、現在 `interactionflow_enabled = True`、
`interactionflow_mode = Error` が指定されています。

## レイヤー依存関係規則

`InteractionFlowArchitecture001` は、namespace に含まれるレイヤー名をもとに依存方向を検査します。
対象レイヤー名は以下です。

- `SystemFlows`
- `Interactions`
- `ExternalPorts`
- `Externals`
- `Builders`
- `Entities`

namespace の各セグメントを大文字小文字非依存で見て、最初に一致したレイヤー名をその型の所属レイヤーとして扱います。
ソース側 namespace が上記レイヤーに属していない場合は、管理対象外として診断しません。

### 許可される主な依存

- 全レイヤーから `Entities` への依存
- `SystemFlows` から `Interactions` への依存
- `Interactions` から `ExternalPorts` への依存
- `Externals` から `ExternalPorts` への依存
- `Builders` から `SystemFlows` / `Interactions` / `ExternalPorts` / `Externals` / `Entities` への依存

上記以外の管理対象レイヤー間依存は診断対象です。

### 外部 namespace への依存

管理対象レイヤー以外の namespace は外部依存として扱います。
ただし、以下は診断しません。

- `interactionflow_allowed_roots` に指定した namespace root とその子 namespace
- 既定で許可される `System`
- `Builders` からの外部依存
- `Externals` からの外部依存

外部依存の許可判定は namespace 境界で行います。
たとえば `ThirdParty` を許可した場合、`ThirdParty.Lib` は許可されますが、`ThirdPartyX` は許可されません。

### 検査する参照形状

定義側と使用側の両方を検査します。

定義側では、以下の型参照を検査します。

- プロパティ型
- フィールド型
- メソッド戻り値型
- メソッド引数型
- 型パラメータ制約
- 基底クラス
- 実装 interface

使用側では、以下の operation から依存先型を検査します。

- オブジェクト生成
- メソッド呼び出し
- フィールド参照
- プロパティ参照
- ローカル変数宣言

ジェネリック型引数、配列要素型、`Nullable<T>` の内側も再帰的に検査します。
同じ解析対象内で同じ型を複数回見つけた場合は、重複診断を避けます。

### メッセージ

共通文と詳細理由を組み合わせて出力します。

```text
Invalid layer dependency: Layer '{0}' must not depend on '{1}'; referenced type: '{2}'
```

日本語リソースでは以下の形式です。

```text
レイヤー依存関係規則に違反しています: '{0}' は '{1}' に依存できません。参照型: '{2}'
```

## IDependencyNode 宣言規則

`InteractionFlowArchitecture002` は、
`InteractionFlow.Core.Entities.Architectures.IDependencyNode` を実装する class を検査します。
interface や struct は対象外です。

### 目的

`IDependencyNode` 系の依存をコンストラクタで受け取る場合、その依存が `Dependency` プロパティで列挙されることを保証します。
また、継承可能なノードでは、派生クラスが依存を追加できるように
`params IDependencyNode[] dependency` を受け取ることを要求します。

### 対象になる dependency 引数

コンストラクタ引数のうち、以下を `IDependencyNode` 系の依存として扱います。

- `IDependencyNode`
- `IDependencyNode` を実装した型
- `IDependencyNode[]`
- `IDependencyNode` 制約を持つ型パラメータ

通常コンストラクタとプライマリコンストラクタの両方を対象にします。

### 非 sealed class の params 要求

`IDependencyNode` 実装 class が `sealed` ではない場合、
各通常コンストラクタまたはプライマリコンストラクタに
`params IDependencyNode[] dependency` が必要です。

`sealed` class は継承拡張の対象外なので、この params 要求は適用しません。

### Dependency への包含チェック

基底 class が `IDependencyNode` 実装ではない場合、
各 `IDependencyNode` 系コンストラクタ引数が `Dependency` に含まれることを検査します。

現在検出できる主な形は以下です。

- `Dependency` プロパティ本体や初期化子が引数を直接参照する
- `Dependency` が参照するフィールドまたはプロパティの宣言が引数を参照する
- コンストラクタ内で、`Dependency` プロパティ自身または `Dependency` が参照するメンバーへ代入し、その右辺が引数を参照する

例:

```csharp
public sealed class Node(IDependencyNode node) : IDependencyNode
{
    private readonly IDependencyNode[] dependency = [node];

    public ReadOnlyMemory<IDependencyNode> Dependency => dependency;
}
```

```csharp
public sealed class Node : IDependencyNode
{
    public Node(IDependencyNode node)
    {
        Dependency = [node];
    }

    public ReadOnlyMemory<IDependencyNode> Dependency { get; }
}
```

### 基底 IDependencyNode への引き渡しチェック

基底 class が `IDependencyNode` 実装の場合、
派生 class の各 `IDependencyNode` 系コンストラクタ引数は、基底コンストラクタへ渡す必要があります。

通常コンストラクタの `: base(...)` と、プライマリコンストラクタの base 指定を検査します。

例:

```csharp
public abstract class ChildNode(
    IDependencyNode node,
    params IDependencyNode[] dependency)
    : BaseNode(node, dependency)
{
}
```

### メッセージ

共通文と詳細理由を組み合わせて出力します。

```text
Invalid dependency node declaration: {detail}
```

詳細理由は主に以下です。

- `IDependencyNode class must be sealed or declare 'params IDependencyNode[] dependency'`
- `Parameter '{0}' must be included in Dependency`
- `Parameter '{0}' must be passed to the base IDependencyNode constructor`

日本語リソースにも同等のメッセージを定義しています。

## 実装構成

- `InteractionFlowAnalyzersAnalyzer.cs`
  - Roslyn Analyzer 本体です。
  - compilation start ごとに `CompilationAnalyzerContext` を作り、オプション、SemanticModel、レイヤー判定結果をキャッシュします。
- `LayerNames.cs`
  - namespace からレイヤー名を抽出し、レイヤー間または外部 namespace への依存可否を判定します。
- `OptionValues.cs`
  - `.editorconfig` から Analyzer オプションを読み取ります。
- `Resources.resx` / `Resources.ja.resx`
  - 診断タイトル、説明、共通文、詳細理由の英語・日本語リソースです。

## 既知の制限

- `IDependencyNode` の包含チェックは、構文上のシンボル参照を追跡する軽量な実装です。
  複雑な制御フロー、別メソッド経由の加工、コレクションへの段階的追加などを完全には追跡しません。
- 基底コンストラクタへの引き渡しチェックは、対象引数が base 呼び出し構文内に参照されているかを見ます。
  引数の意味的な変換結果までは検証しません。
- レイヤー判定は namespace 名に含まれる既定レイヤー名に依存します。
  レイヤー名を含まない namespace は管理対象外として扱います。

## 検証

Analyzer 単体の確認:

```powershell
dotnet test InteractionFlow.Analyzers.Tests/InteractionFlow.Analyzers.Tests.csproj
```

ソリューション全体の確認:

```powershell
dotnet build InteractionFlow.slnx
dotnet test InteractionFlow.slnx
```
