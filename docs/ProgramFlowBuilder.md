[Readme](../README.md#programflow-builder-block)

 ---

# ProgramFlow Builder の詳細

本ドキュメントでは、Interaction Flow Architecture における **ProgramFlow Builder** の構造と設計意図について説明します。

---

## 概要

ProgramFlow Builder は、**ProgramFlow の実行単位とその依存関係を構築・管理するための仕組み**です。

本アーキテクチャでは、Dependency Injection（DI）とライフタイム管理を明確に分離し、
**スコープ単位での依存関係管理**を可能にしています。

この仕組みは、以下の2つのビルダーによって構成されます：

* ScopeBuilder
* ProgramFlowBuilder

ScopeBuilder は再利用可能なスコープの構築に使用され、  
ProgramFlowBuilder は再利用可能な ProgramFlow の実行単位を構築するために使用されます。

---

## ScopeBuilder

### 役割

ScopeBuilder は、**独立したライフタイムを持つスコープ**を構築するためのビルダーです。

* 事前に登録された型情報をもとに Dependency Injection を構成
* スコープのライフタイムを管理する `ScopeHandler` を生成

さらに、親となるスコープを指定することで、**複数スコープ間の依存関係を統合**できます。

### インターフェース

```csharp
public interface IScopeBuilder : IScopeServices
{
    ScopeHandler BuildScope(params ScopeHandler[] parents);
    ...
}
```

### 依存関係の解決

スコープは親子関係を持つことができ、依存関係は以下の優先順位で解決されます：

> 子スコープ ＞ 親スコープ

これにより、親スコープの定義をベースにしつつ、**部分的な上書きや差し替え**が可能になります。

```text
1.      親スコープで IStorage を FileStorage として登録
2.      子スコープで IStorage を InMemoryStorage として再登録

結果：  子スコープ内では InMemoryStorage が使用される
```
---

## ProgramFlowBuilder

### 役割

ProgramFlowBuilder は、**ProgramFlow とその実行スコープを一体として構築・管理するビルダー**です。

* ProgramFlow に対応するスコープを生成
* ProgramFlow とスコープのライフタイムを一致させる
* Dependency Injection を適用して `ProgramFlowHandler` を生成

### インターフェース

```csharp
public interface IProgramFlowBuilder<TContext> : IScopeServices
    where TContext : IFlowContext
{
    ProgramFlowHandler<TContext> BuildProgramFlow<TProgramFlow>(params ScopeHandler[] parents)
        where TProgramFlow : IProgramFlow<TContext>;
    ...
}
```

### 特徴

* ProgramFlow のライフタイムはスコープと完全に一致する
* 親スコープを指定することで、外部依存や共有リソースを注入できる
* ScopeBuilder と同様に、依存関係は「子優先」で解決される

---

## ライフタイムと破棄

ProgramFlowHandler および ScopeHandler は、それぞれが管理するスコープのライフタイムを持ちます。  
これらは `IDisposable` を実装しており、**開発者が明示的に破棄する必要があります**。

```csharp
using var programFlow = programFlowBuilder.BuildProgramFlow<SomeProgramFlow>(...);
```

スコープの破棄により、そのスコープに直接属する依存オブジェクトも同時に解放されます。
（親スコープに含まれるオブジェクトは、その親スコープが破棄されるまで破棄されません）

---

## ProgramFlowHandlerを通じたProgramFlowの実行

ProgramFlowHandler は、生成された ProgramFlow インスタンスを内部に保持し、その ProgramFlow によって定義されたユーザーフローを実行する責務を持ちます。

```csharp
//endToken は、フローの終了状態（例：正常終了/キャンセル）および新しい Context を持つオブジェクト
var endToken = await programFlow.ExecuteAsync(currentContext);
```

これにより：

- ProgramFlow は純粋にフローの定義に集中できる
- 実行環境（依存関係・ライフタイム）は Handler 側に分離される

という役割分担が実現されます。

---

## スコープ構造と依存関係

ScopeBuilder / ProgramFlowBuilder は、スコープを**単なるツリーではなくグラフ構造**として扱います。

```text
        [Parent Scope A]
               ↑
        [Parent Scope B]
               ↑
           [ProgramFlow Scope]
```

この構造により：

* 複数のスコープを合成できる
* 共通依存を共有しつつ、個別の差し替えが可能
* 柔軟な依存関係構築が可能

ただし、循環参照はエラーとして扱われます。

---

## 設計意図

### 1. 再利用性の確保

* ProgramFlow を再利用したい
* Function（外部実装）も可能であれば再利用したい

しかし、自動的なライフタイム管理に依存すると：

* スコープの境界が不明確になる
* 意図しない共有や破棄タイミングが発生する

これを回避するために、

- **ライフタイムを明示的に制御可能な DI 構造を採用**

---

### 2. ライフタイムのグループ管理

* 複数の依存オブジェクトをまとめて管理したい
* ProgramFlow 独自の依存オブジェクトは ProgramFlow と同一のライフタイムで扱いたい

これを実現するために、

- **スコープ単位でライフタイムを管理**
- **ProgramFlow 独自の依存オブジェクトは ProgramFlow と束ねて同期**

---

### 3. 部分ビルドの可能性

本アーキテクチャでは、依存関係が一方向かつ隣接した層に制限されています：

> ProgramFlow → Interaction → Function Port ← Function External

この性質により：

* 一部のスコープのみを構築する「部分ビルド」が可能
* 必要な依存だけを組み合わせて実行できる

さらに、親スコープを指定できることで：

* 既存スコープの再利用
* 差分追加による構築

といった柔軟な構成が可能になります。

例えば、Scope Builder を用いて共通の Function を親として構築し、
ProgramFlow Builder を用いて個別の ProgramFlow ごとに差分のみを持つスコープを生成する、といった使い方が可能です。

---

## まとめ

ProgramFlow Builder は、以下の特徴を持つ構築機構です：

* スコープ単位での Dependency Injection
* ProgramFlow と ProgramFlow 内スコープのライフタイム同期
* 親子関係による依存関係の合成
* 子優先の依存解決
* 部分ビルドによる高い再利用性

これにより、本アーキテクチャにおける「構造の明確さ」と「実装の柔軟性」を両立しています。

---
[Readme](../README.md#programflow-builder-block) | [PageTop](#programflow-builder-の詳細) 
