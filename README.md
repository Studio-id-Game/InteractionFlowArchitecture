# Interaction Flow C# Package

このプロジェクトは、Interaction Flow Architecture を C# で実現するためのベースライブラリです。

## 目次
[全体構成](#全体構成) 
| [Layers](#layers)
| [Blocks](#blocks)
| [フローと依存関係](#フローと依存関係)
| [概念モデル](#概念モデル)
| [振る舞いの違い](#振る舞いの違い)
| [制約とアンチパターン](#制約とアンチパターン)
| [設計指針（名前空間）](#設計指針名前空間)

## Interaction Flow Architecture

本プロジェクトが提唱する Interaction Flow Architecture は、クリーンアーキテクチャと同様の高いテスト耐性と拡張性を備えています。

さらに、構造の認知しやすさと責任範囲の明確化を徹底することで、実装単位や責務、コードの配置が自然に導かれるよう設計されています。  
開発者は「どこに何を書くべきか」を意識的に判断する必要がなくなり、設計の迷いを大きく減らすことができます。  

また、この構造に従うことで、UX を損なわないフロー設計を行いやすくなります。

## このアーキテクチャが向いているケース

- 利用側とシステムの相互作用ベースのアプリケーション
  >（対話的なアプリケーション、フレームループをもったアプリケーション等）

- フローが複雑になりやすいシステム
  >（ゲームシステム、複雑なコンテキストの更新をともなうシステム等）

- UI / DB / 外部サービスの分離が重要な場合
  >（複数の外部APIやデータストアを扱い、テストや差し替えが困難になりやすいシステム等）

## ソリューション構成

本リポジトリは、以下のプロジェクトで構成されています。
- `InteractionFlow.Core`  
  基礎となるインターフェースや構造を提供するライブラリ

- `InteractionFlow.Standard`  
  コンソール操作など、汎用的な実装を提供するライブラリ

- `InteractionFlow.Analyzers`  
  アーキテクチャの依存関係ルールを検証し、設計違反を検出する Roslyn アナライザー

- `InteractionFlow.Sample.Parrot`  
  コンソールベースのオウム返しアプリケーションによる、基本構成のサンプル実装

---

# 全体構成

本アーキテクチャは、以下の要素で構成されます：

- 4つの Layer（層）
- 3つの Block（補助構造）

各要素は、それぞれ対応する名前空間（およびディレクトリ）を持ちます。

以下は、本アーキテクチャの構造の全体像です。

![Architecture Overview](./docs/img/Interaction_Flow_Architecture__Overview.png)

---

# Layers

## Focus Layer

**namespace**  
`ProjectName.Focuses.{FocusName}`

**役割**  
ユーザーの目的を達成するためのフロー単位です。  
ユーザーにとって「単一の意味」を持つ単位として設計されます。

**構成**  
- 1つのユーザー目的に対して1つのクラス（または構造体）で構成

## Interaction Layer

**namespace**  
`ProjectName.Interactions.{InteractionName}`

**役割**  
システム内部の目的を達成するためのフロー単位です。  
システムにとって「単一の意味」を持つ処理単位として設計されます。

**特徴**

- Port 層を経由して機能を呼び出す
- ユーザー入力を受け取り、処理に適用する
- 保存操作やユーザーへの反応を実行する

**構成**  
- 1つのシステム目的に対して1つのクラス（または構造体）で構成

### Interaction Rules

**namespace**  
`ProjectName.Interactions.Rules.{InteractionRuleName}`

**役割**  
複数の Interaction 間で共有されるべきルールを定義します。

**制約**
- `ProjectName.Interactions` 内からのみ参照可能

## Function Port Layer

**namespace**  
`ProjectName.{Operation|Storage|Reaction|SilentIntegration}Ports.{PortName}`

**役割**  
依存関係を逆転させるための抽象インターフェース群です。

**特徴**
- 外部機能を interface として定義
- 実装の差し替えを可能にする

## Function External Layer

**namespace**  
`ProjectName.{Operations|Storages|Reactions|SilentIntegrations}.{ExternalFunctionName}`

**役割**  
実際の処理を行う、外部依存の実装です。

**分類**

- **Operations**  
  ユーザー入力や条件の取得を担当（UI / Controller 相当）

- **Storages**  
  状態の保存・管理を担当（DB / FileSystem / Gateway 相当）

- **Reactions**  
  ユーザーに観測可能な出力を担当（UI / Presenter 相当）

- **SilentIntegration**  
  ユーザーに観測されない、外部実行環境との結合を担当（Service 相当）

---

# Blocks

## Domain Block

**namespace**  
`ProjectName.Entities.{EntityName}`

**役割**  
システムの前提となるデータ構造（エンティティ）を定義します。

### Entity Rules

**namespace**  
`ProjectName.Entities.Rules.{EntityRuleName}`

**役割**  
エンティティに対する制約やルールを定義します。

**制約**
- `ProjectName.Entities` 内からのみ参照可能

## Focus Builder Block

**namespace**  
`ProjectName.Builders`

**役割**  
DI コンテナのラッパーとして、Focus の構築を担います。

**特徴**
- Function Port を介して Function External 実装を注入
- Focus の実行環境を構成する

> 詳細な構築手順や設計意図については、[Focus Builder の詳細](./docs/FocusBuilder.md) を参照してください。

## External Block

**役割**  
OS、Framework、ライブラリなどの外部要素です。

※本アーキテクチャの管理対象外

---

# フローと依存関係

このアーキテクチャでは、依存関係の逆転により実行フローと依存関係が明確に分離されます。

## 実行フローとContext（文脈）

以下は、本アーキテクチャにおけるフローの全体像です。

![Architecture Flow](./docs/img/Interaction_Flow_Architecture__User_Flow.png)

ユーザー視点の処理は、以下の順で流れます：

> User(開始) → Focus → Interaction → Function Port → Function External → User(入力/観測/終了)

### Context（文脈）
Focus からエントリーするユーザー視点のフローは、常に「Context（文脈）」を入力として開始されます。

Context は現在のユーザーに関する状態や状況を表す文脈的情報で、初期に与えられた情報や、過去の処理によって更新された情報を含みます。  
Focus はこの Context をもとに実行され、Interaction を通じて処理が進行します。

処理の過程で、Function External を介した操作や状態更新が行われ、その結果として Context は更新されます。  
また、Context の更新は Focus / Interaction 内で行われる場合もあります。

更新された Context は、必要に応じて次のフローの入力として再利用されます。  
これにより、連続したユーザー体験が構成されます。

## 依存関係

以下は、本アーキテクチャにおける依存関係の全体像です。

![Architecture Flow](./docs/img/Interaction_Flow_Architecture__Dependency_Diagram.png)

依存関係は次のようになります：

- Focus は Interaction に、Interaction と Function External は Function Port に依存します

> Focus → Interaction → Function Port ← Function External

- Function External のみが外部に依存します

> Function External → External Block

- Builder は  Interaction と External Block を除くすべての要素に依存します

> Focus Builder → Focus / Function Port / Function External

- すべての要素は Domain に依存します

> All Layers & Blocks → Domain

---

# 概念モデル

## Focus / Interaction / Function

### Focus（焦点）

ユーザーにとっての意味単位です。  
複数の Interaction を組み合わせてユーザーフローを構成します。

### Interaction（作用）

システム内部の意味単位です。  
Function Port を介して複数の Function（機能）を実行し、それらを組み合わせてシステムフローを構成します。

### Function（機能）

処理の実体です。

- Operation（ユーザーからの入力）
- Storage（状態管理）
- Reaction（ユーザーへの出力）
- SilentIntegration（ユーザーに見えない、外部実行環境とのやりとり）

これらは Function Port によって抽象化され、Function External によって実装されます。

## 計算モデルとしての Interaction Flow アーキテクチャ

本アーキテクチャは、状態遷移とテープ操作を持つチューリングマシンのモデルとして解釈することもできます。
この視点では、Interaction は状態遷移、Function はテープ操作として捉えることができます。
このことは、アーキテクチャとしての構造の必要十分性を保証します。

> 詳細な解釈については、[計算モデルとしての Interaction Flow アーキテクチャ](./docs/ComputationalModel.md) を参照してください。


# 振る舞いの違い

## 中断

- Function は中断（例外・キャンセル）を持つ
- Focus / Interaction は中断を持たない

> 正確には、Focus / Interaction は中断を適切に完了し、その結果をユーザーに伝えることで、常に正常終了として扱う

## 状態
- Function（Operation / Storage / Reaction）のみが、必要に応じて Mutable な状態を持つことができる
- Focus / Interaction は Immutable

また、Focus / Interaction はフロー中の遷移状態は持てますが、フローのスコープ終了時に必ず破棄されます。

---

# 制約とアンチパターン

## Focus の制約

- Function Port および Function External に依存しない
- ユーザーにとって単一の意味と目的を持つ
- 必ず「明確な終了」を示す Interaction を持つ

### アンチパターン

- ユーザーの目的を意味しない Focus

> ユーザーの目的と対応する場合は、単一の Interaction をラップする Focus であってもよい。

## Interaction の制約

- Function Port に依存するが、Function External には依存しない
- Focus に依存しない
- システム内で単一の意味と目的を持つ
- 必ず終了を示す Reaction を持つ（例外やキャンセルも含めて最終的にユーザーに結果を返す）

### アンチパターン

- 過度に巨大な Interaction

> システムの目的と対応する場合は、単一の Reaction をラップする Interaction であってもよい。
> また、それ以上分解するとシステム内における意味や目的が失われる場合には、複数の Function を組み合わせてある程度大きい Interaction を構築してもよい。

## 補足

Focus / Interaction の粒度はチームで調整可能です。
ただし、前述したアンチパターンにならないように注意する必要があります。

---

# 設計指針（名前空間）

各 Layer / Block は、名前空間およびディレクトリ構造と一致させることを推奨します。

さらに、アルファベット順に並べることで、おおよそ処理フロー順に配置されます。

これにより：

- 構造とコードの対応が明確になる
- 認知負荷が下がる
- 保守性が向上する

---
[PageTop](#) 
| [全体構成](#全体構成) 
| [Layers](#layers)
| [Blocks](#blocks)
| [フローと依存関係](#フローと依存関係)
| [概念モデル](#概念モデル)
| [振る舞いの違い](#振る舞いの違い)
| [制約とアンチパターン](#制約とアンチパターン)
| [設計指針（名前空間）](#設計指針名前空間)