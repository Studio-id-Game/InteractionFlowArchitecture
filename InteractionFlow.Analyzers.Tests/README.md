<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリ実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |

---

# InteractionFlow.Analyzers.Tests

`InteractionFlow.Analyzers` の Roslyn Analyzer テストプロジェクトです。
`Microsoft.CodeAnalysis.CSharp.Testing` と xUnit を使い、インメモリの C# ソースと `.editorconfig` を渡して診断結果を検証します。

## テスト対象

現在のテストは `InteractionFlowAnalyzersAnalyzer` の 2 つの診断を対象にしています。

| ID | 内容 |
| --- | --- |
| `InteractionFlowArchitecture001` | レイヤー依存関係規則 |
| `InteractionFlowArchitecture002` | `IDependencyNode` 宣言規則 |

## テスト基盤

主なテストコードは `UnitTest1.cs` にあります。

- `VerifyAsync`
  - `CSharpAnalyzerTest<InteractionFlowAnalyzersAnalyzer, DefaultVerifier>` を組み立てます。
  - テストごとにインメモリ `.editorconfig` を追加します。
  - `interactionflow_enabled`、`interactionflow_mode`、`interactionflow_allowed_roots` を指定できます。
- `ExpectedHidden`
  - `InteractionFlowArchitecture001` の Hidden 診断期待値を作ります。
- `ExpectedDependencyHidden`
  - `InteractionFlowArchitecture002` の Hidden 診断期待値を作ります。
- `ExpectedLayerDependencyDetail`
  - `Resources.LayerDependencyDisallowedReference` を使って、レイヤー診断の詳細理由を生成します。
- `InMemoryAnalyzerConfigOptions`
  - `OptionValues` の allowed roots 正規化など、オプション読み取りロジックの直接テストに使います。

重大度は `interactionflow_mode` に合わせて `.editorconfig` の
`dotnet_diagnostic.<ID>.severity` も同時に設定します。

## レイヤー依存関係規則のテスト

### レイヤー間依存

管理対象レイヤー間の許可・禁止を検証します。

- `SystemFlows` から `ExternalPorts` / `Externals` / `Builders` への禁止依存
- `Interactions` から `SystemFlows` / `Externals` / `Builders` への禁止依存
- `ExternalPorts` から `SystemFlows` / `Interactions` / `Externals` / `Builders` への禁止依存
- `Entities` から他の管理対象レイヤーへの禁止依存
- 各レイヤーから許可された管理対象レイヤーへの依存が診断されないこと

### 外部 namespace

管理対象外 namespace への依存について、以下を検証します。

- `Interactions` などから許可されていない外部 namespace へ依存すると診断されること
- `System` は既定で許可されること
- `interactionflow_allowed_roots` に指定した root と子 namespace は許可されること
- `ThirdParty` と `ThirdPartyX` のような部分一致は許可されないこと
- `Builders` からの外部依存は診断されないこと

### 参照形状

定義側と使用側のさまざまな C# 形状から依存先型を検出できることを検証します。

- メソッド戻り値型
- メソッド引数型
- 型パラメータ制約
- 基底クラス
- interface 実装
- フィールド型
- プロパティ型
- オブジェクト生成
- メソッド呼び出し
- フィールド参照
- プロパティ参照
- ローカル変数宣言

### 複合型と重複診断

型の再帰検査と重複抑制を検証します。

- ジェネリック型引数内に同じ禁止型が複数回現れても 1 件だけ診断すること
- `Nullable<T>` や配列の内側にある禁止型を診断すること
- タプル、匿名型、ラムダ、ローカル関数などを含む許可済み依存を誤診断しないこと
- global namespace を持つ合成型を外部依存として誤診断しないこと

### オプション

Analyzer オプションの動作を検証します。

- `interactionflow_enabled = False` で診断しないこと
- `interactionflow_mode = Error` で Error 診断になること
- 不正な `interactionflow_mode` は Warning にフォールバックすること
- `interactionflow_allowed_roots` は大文字小文字非依存で重複を正規化すること

## IDependencyNode 宣言規則のテスト

テスト内で最小の
`InteractionFlow.Core.Entities.Architectures.IDependencyNode` 定義を追加し、
Analyzer が metadata name で対象 interface を解決できるようにしています。

### 正常系

以下のケースで診断しないことを検証します。

- 通常コンストラクタで受け取った `IDependencyNode` 系引数を、`Dependency` が返す配列に含める
- プライマリコンストラクタで受け取った `IDependencyNode` 系引数を、`Dependency` が返す配列に含める
- 通常コンストラクタ内で `Dependency` auto-property に直接代入する
- `sealed` class が `params IDependencyNode[] dependency` を持たない

### 欠落検出

以下のケースで `InteractionFlowArchitecture002` を診断することを検証します。

- 通常コンストラクタで受け取った `IDependencyNode` 系引数が `Dependency` に含まれない
- `IDependencyNode` 実装 class を継承するプライマリコンストラクタで、引数を基底コンストラクタへ渡さない
- `abstract` class のコンストラクタに `params IDependencyNode[] dependency` がない
- `sealed` ではない具象 class のコンストラクタに `params IDependencyNode[] dependency` がない

### ローカライズ

`DependencyNodeResources_LocalizesMessages` で、`Resources.Culture` を切り替え、
Dependency Node 系メッセージが英語・日本語で取得できることを確認します。

## 実行方法

Analyzer テストだけを実行する場合:

```powershell
dotnet test InteractionFlow.Analyzers.Tests/InteractionFlow.Analyzers.Tests.csproj
```

ソリューション全体で確認する場合:

```powershell
dotnet build InteractionFlow.slnx
dotnet test InteractionFlow.slnx
```

---

<!--- ヘッダ・フッタのリンク表示文字列は、例外的な省略記法を維持する --->
[README](./../README.md) |
[Philosophy](./../docs/PHILOSOPHY.md) |
[計算モデルとして](./../docs/COMPUTATIONAL_MODEL.md) |
[ライブラリの実装](./../docs/LIBRARY_IMPLEMENTATION.md) |
[ライブラリ実装の詳細](./../docs/LIBRARY_IMPLEMENTATION_DETAIL.md) |
