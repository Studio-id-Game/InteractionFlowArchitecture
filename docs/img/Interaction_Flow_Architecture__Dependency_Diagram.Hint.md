# Interaction Flow Architecture - Dependency Diagram

本ドキュメントは、Interaction Flow Architecture における依存関係とレイヤー構造をテキストとして表現したものである。

---

## 概要

システムは以下のブロックで構成される：

- **Focus Builder Block**
- **Layers（中核レイヤー群）**
  - Focus Layer
  - Interaction Layer
  - Function Port Layer
  - Function External Layer
- **Domain Block**
- **External Block**

---

## 1. Focus Builder Block

- 役割：
  - Focus を構築する DI コンテナのラッパー
- 機能：
  - 実装の選択
  - DI によるインスタンス化
- 参照：
  - Function Port Layer の定義

---

## 2. Layers

### 2.1 Focus Layer

- 構成要素：
  - `Focus`
- 役割：
  - ユーザーの目的を達成するためのフロー単位
- 特徴：
  - 複数の Interaction を組み合わせてユーザーフローを構築

---

### 2.2 Interaction Layer

- 構成要素：
  - `Interaction`
  - `Interactions.Rules`
- 役割：
  - システム内部の目的を達成するためのフロー単位
- 特徴：
  - Focus によって組み合わせられる
  - フロー整合性のため Rules を遵守

- Interaction が扱う責務：
  - ユーザーからの入力待機
  - ユーザーへの反応待機
  - 外部実行環境との結合
  - 保管操作待機

---

### 2.3 Function Port Layer

- 構成要素（抽象インターフェース）：
  - `OperationPort`
  - `ReactionPort`
  - `SilentIntegrationPort`
  - `StoragePort`

- 役割：
  - 依存関係を逆転させるための抽象層

- 各 Port の意味：
  - **OperationPort**
    - ユーザー入力の実行
  - **ReactionPort**
    - ユーザーへの反応の実行
  - **SilentIntegrationPort**
    - 外部環境との非対話的結合
  - **StoragePort**
    - 永続化・一時保存の操作

---

### 2.4 Function External Layer

- 構成要素（具体実装）：
  - `Operation`
  - `Reaction`
  - `SilentIntegration`
  - `Storage`

- 役割：
  - 実際の処理を行う外部依存の実装

- 対応関係：
  - OperationPort → Operation
  - ReactionPort → Reaction
  - SilentIntegrationPort → SilentIntegration
  - StoragePort → Storage

---

## 3. Domain Block

- 構成要素：
  - `Entities`
  - `Entities.Rules`

- 役割：
  - システムの前提となるデータ構造の定義

- 特徴：
  - 各レイヤーから参照される
  - 整合性維持のため Rules を持つ

---

## 4. External Block

- 構成要素：
  - OS
  - Framework
  - Library

- 役割：
  - 技術基盤（外部依存）
  - Function External Layer から利用される

---

## 依存関係まとめ

### 上位 → 下位の依存

- Focus
  → Interaction

- Interaction
  → Function Port

- Function Port
  ← Function External（実装はDIで注入）

- 全レイヤー
  → Domain（Entities）

- Function External
  → External Block（OS / Framework / Library）

---

### 依存性逆転（重要）

- Interaction は具体実装に依存しない
- Port（抽象）にのみ依存する
- 実装は Focus Builder により注入される

---

## 補足ルール

- 破線矢印は依存関係を表す
- Operation / Reaction は UI の入出力に対応
- Storage の対象：
  - DB
  - ファイルシステム
  - 環境設定
  - （永続 / 一時を問わない）

---

## 概念まとめ（短縮）

- Focus：
  - ユーザー目的単位

- Interaction：
  - 内部処理フロー単位

- Port：
  - 外部との境界（抽象）

- External：
  - 実処理

- Domain：
  - データ定義と整合性

- Builder：
  - 依存解決と構成

---

## 構造の本質

このアーキテクチャは：

- フロー（Focus / Interaction）と
- 実装（External）を分離し
- Port によって接続する

ことで、

**「意味の流れ」と「実行の詳細」を分離した設計**になっている。
