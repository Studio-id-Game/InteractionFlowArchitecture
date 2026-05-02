# Interaction Flow Architecture - User Flow 

本ドキュメントは、Interaction Flow Architecture におけるユーザーフローの構造と処理の流れをテキストとして表現したものである。 

---

## 概要

Interaction Flow Architecture は、ユーザー（または外部システム）との相互作用を中心に、  
**Context の生成・更新を軸として処理が循環する構造**を持つ。

全体は以下の要素で構成される：

- User（外部主体）
- Current Context / New Context
- Focus Builder Block
- External Block
- 各種 Layer（Focus / Interaction / Function Port / Function External）
- Domain Block

処理は「Context → Flow実行 → 外部作用 → Context更新」というループとして進行する。

---

## 全体フロー

### 1. Context の構築 + フローの開始

- 入力：Current Context
- 処理：
  - Focus Builder Block が Context をもとにフローを構築
  - DIコンテナのラッパーとして機能
- 出力：
  - Focus のインスタンス化
  - フローの実行開始

---

### 2. 操作の入力 + 反応の観測（相互作用の実現）

- User が操作を入力
- システムは反応を返す
- External Block が実際の外部処理を担う

External Block の内容：
- 外部ライブラリ
- OS
- フレームワーク
- その他技術要素

---

### 3. 新しい Context の提供

- 各処理の結果として Context が更新される
- New Context が生成され、次のループへ渡される

---

## 内部レイヤー構造

Layers はフロー実行の責務分離を行う。

### 1. Focus Layer
- 役割：ユーザーの目的を達成するためのフロー単位
- 特徴：
  - 外部から見た意味単位
  - ユーザーフローの実行を担う

---

### 2. Interaction Layer
- 役割：システム内部の目的を達成するフロー単位
- 特徴：
  - Focus を構成する内部処理
  - システムフローの実行を担う

---

### 3. Function Port Layer
- 役割：依存関係を逆転させる抽象インターフェース
- 特徴：
  - 外部実装へのポート
  - 抽象化境界

---

### 4. Function External Layer
- 役割：実際の処理を行う外部依存の実装
- 特徴：
  - インフラ・技術スタックへの接続
  - 外部機能の実行を担う

---

## Domain Block

- 役割：システムの前提となるデータ構造の定義
- 特徴：
  - 全レイヤーから参照される
  - ビジネスロジックの基盤

---

## ブロック構造

### Focus Builder Block
- Context から Focus を構築
- DIコンテナのラッパー
- フローの起点

### External Block
- 技術選定された外部要素群
- 実処理の実行主体

---

## Context のライフサイクル

1. Current Context が入力される
2. フローが実行される
3. 外部との相互作用が行われる
4. Context が更新される
5. New Context が生成される
6. 次の処理へ引き継がれる

---

## フローの種類

- ユーザーフロー（Focus Layer）
- システムフロー（Interaction Layer）
- 外部機能実行（Function External Layer）

---

## 矢印の意味

- ピンク：
  - ユーザーフロー（時間の流れ）
- ブルー：
  - 処理の実行
- オレンジ：
  - Context の更新

---

## 補足

- User には以下が含まれる：
  - 人間
  - 上位システム
  - エージェント
  - 他システム

- 本アーキテクチャは、
  **「相互作用によって意味が生成される構造」**として設計されている。
