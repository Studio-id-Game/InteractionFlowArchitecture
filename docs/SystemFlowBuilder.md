[Readme](../README.md#implementation)

 ---

# SystemFlow Builder の詳細 <a id="systemflow-builder"></a>

本ドキュメントでは、Interaction Flow Architecture における **SystemFlow Builder** の構造と設計意図について説明します。

---

## 概要

SystemFlow Builder は、**SystemFlow の実行単位とその依存関係を構築・管理するための仕組み**です。

本アーキテクチャでは、Dependency Injection（DI）とライフタイム管理を明確に分離し、
**スコープ単位での依存関係管理**を可能にしています。

この仕組みは、以下の2つのビルダーによって構成されます：

* ScopeBuilder
* SystemFlowBuilder

ScopeBuilder は再利用可能なスコープの構築に使用され、  
SystemFlowBuilder は再利用可能な SystemFlow の実行単位を構築するために使用されます。

どちらの Builder も、一度 Build すると同じインスタンスを再利用できません。
再利用されるのは、Builder が生成した Scope や SystemFlow の実行単位です。

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

## SystemFlowBuilder

### 役割

SystemFlowBuilder は、**SystemFlow とその実行スコープを一体として構築・管理するビルダー**です。

* SystemFlow に対応するスコープを生成
* SystemFlow とスコープのライフタイムを一致させる
* Dependency Injection を適用して `SystemFlowHandler` を生成

### インターフェース

```csharp
public interface ISystemFlowBuilder<TContext> : IScopeServices
    where TContext : IFlowContext
{
    SystemFlowHandler<TContext> BuildSystemFlow<TSystemFlow>(params ScopeHandler[] parents)
        where TSystemFlow : ISystemFlow<TContext>;
    ...
}
```

### 特徴

* SystemFlow のライフタイムはスコープと完全に一致する
* 親スコープを指定することで、外部依存や共有リソースを注入できる
* ScopeBuilder と同様に、依存関係は「子優先」で解決される

---

## ライフタイムと破棄

SystemFlowHandler および ScopeHandler は、それぞれが管理するスコープのライフタイムを持ちます。  
これらは `IDisposable` を実装しており、**開発者が明示的に破棄する必要があります**。

```csharp
using var systemFlow = systemFlowBuilder.BuildSystemFlow<SomeSystemFlow>(...);
```

スコープの破棄により、そのスコープに直接属する依存オブジェクトも同時に解放されます。
（親スコープに含まれるオブジェクトは、その親スコープが破棄されるまで破棄されません）

---

## SystemFlowHandlerを通じたSystemFlowの実行

SystemFlowHandler は、生成された SystemFlow インスタンスを内部に保持し、その SystemFlow によって定義された System 側のフローを実行する責務を持ちます。

```csharp
//endToken は、フローの終了状態（例：正常終了/キャンセル）および実行に渡した Context を持つオブジェクト
var endToken = await systemFlow.ExecuteAsync(currentContext);
```

これにより：

- SystemFlow は純粋にフローの定義に集中できる
- 実行環境（依存関係・ライフタイム）は Handler 側に分離される

という役割分担が実現されます。

---

## スコープ構造と依存関係

ScopeBuilder / SystemFlowBuilder は、スコープを**単なるツリーではなくグラフ構造**として扱います。

```text
        [SystemFlow Scope]
          ├─ Parent Scope A
          └─ Parent Scope B
```

この構造により：

* 複数のスコープを合成できる
* 共通依存を共有しつつ、個別の差し替えが可能
* 柔軟な依存関係構築が可能

自身のスコープで解決できないサービスは、`parents` に渡した順で親スコープを探索します。
複数の親が同じサービスを提供する場合は、最初に解決できた親のサービスが使われます。

親スコープのグラフに循環がある場合、`ScopeHandler` は訪問済みスコープの探索を打ち切り、
その経路ではサービスを未解決として扱います。循環自体を専用エラーとして報告するわけではありません。
探索対象の親スコープがすでに破棄されている場合は例外が発生します。

---

## 設計意図

### 1. 再利用性の確保

* SystemFlow を再利用したい
* Function（外部実装）も可能であれば再利用したい

しかし、自動的なライフタイム管理に依存すると：

* スコープの境界が不明確になる
* 意図しない共有や破棄タイミングが発生する

これを回避するために、

- **ライフタイムを明示的に制御可能な DI 構造を採用**

---

### 2. ライフタイムのグループ管理

* 複数の依存オブジェクトをまとめて管理したい
* SystemFlow 独自の依存オブジェクトは SystemFlow と同一のライフタイムで扱いたい

これを実現するために、

- **スコープ単位でライフタイムを管理**
- **SystemFlow 独自の依存オブジェクトは SystemFlow と束ねて同期**

---

### 3. 部分ビルドの可能性

本アーキテクチャにおける主要な組み立て経路は次のとおりです：

> SystemFlow → Interaction → Function Port ← Function External

Domain への依存や、Builder が各要素を生成するための依存は、この経路とは別に存在します。
実際に許可される namespace 間の依存方向は Analyzer の規則に従います。

この構造により：

* 一部のスコープのみを構築する「部分ビルド」が可能
* 必要な依存だけを組み合わせて実行できる

さらに、親スコープを指定できることで：

* 既存スコープの再利用
* 差分追加による構築

といった柔軟な構成が可能になります。

例えば、Scope Builder を用いて共通の Function を親として構築し、
SystemFlow Builder を用いて個別の SystemFlow ごとに差分のみを持つスコープを生成する、といった使い方が可能です。

---

## まとめ

SystemFlow Builder は、以下の特徴を持つ構築機構です：

* スコープ単位での Dependency Injection
* SystemFlow と SystemFlow 内スコープのライフタイム同期
* 親子関係による依存関係の合成
* 子優先の依存解決
* 部分ビルドによる高い再利用性

これにより、本アーキテクチャにおける「構造の明確さ」と「実装の柔軟性」を両立しています。

---
[Readme](../README.md#implementation) | [PageTop](#systemflow-builder)
