# Interaction Flow C# Package

このプロジェクトは、Interaction Flow Architecture を実現するための、C# 向けベースライブラリです。

# Interaction Flow Architecture

Interaction Flow Architecture は、
クリーンアーキテクチャのようなテスト耐性と依存関係の逆転を実現しながら、
開発者にとって認知・管理しやすい構造や、責任の所在の明確化によって、開発における迷いを減らすアーキテクチャです。

Interaction Flow Architecture の基本構成は、
以下の4つの Layer と3つの Block で構成され、
またそれぞれに固有の名前空間（及びディレクトリ）を持ちます。

## Focus Layer

namespace : `ProjectName.Focuses.{FocusName}`

- ユーザー内の目的を達成するための、ユーザーにとって単一の意味を持つフロー
- ユーザー内の目的ごとに、複数のクラス（または構造体）を実装する

## Interaction Layer

namespace + class name : `ProjectName.Interactions.{InteractionName}`

- システム内の目的を達成するための、システム内部において単一の意味を持つフロー
- システム内の目的ごとに、複数のクラス（または構造体）を実装する
- 接続層による抽象化を経由して、ユーザー入力を解釈し、保管や反応を実行する。

namespace + class name : `ProjectName.Interactions.Rules.{InteractionRuleName}`

- システム内の目的を達成するために定義・遵守する必要がある、フローの規則
- `ProjectName.Interactions` 内からのみ参照可能

## Function Port Layer

namespace + class name : `ProjectName.{Operation|Storage|Reaction}Ports.{PortName}`

- 依存を逆転させるための、機能の抽象的定義
- 外部機能層をinterfaceによって抽象化し、外部実装の切り替えを可能にする

## Function External Layer

namespace + class name : `ProjectName.{Operations|Storages|Reactions}.{ExternalFunctionName}`

- 動作を実現するための、外部に依存した処理
- `Operations` : 具体的な実装によって、ユーザーによる入力や条件を待機、入力・条件データを取得する操作機能を定義する（Controllerに相当）
- `Storages` : 具体的な実装によって、ユーザーまたはアプリケーションに帰属する状態の管理機能を定義する（Gateway + DB/FileSystemなど に相当）
- `Reactions` : 具体的な実装によって、ユーザーに観測可能な形で表現する反応機能を定義する（Presenter + UI に相当）

## Domain Block

namespace + class name : `ProjectName.Entities.{EntityName}`

- システムの前提を構築するための、情報の表現

namespace + class name : `ProjectName.Entities.Rules.{EntityRuleName}`

- システムの前提を構築するために定義・遵守する必要がある、表現の規則
- `ProjectName.Entities` 内からのみ参照可能

## Focus Builder Block

namespace + class name : `ProjectName.Builders`

- 具体的な外部機能層を指定し、Port層を経由して焦点層のコンストラクトを行うための、DIコンテナの特化ラッパー

## External Block

namespace + class name : `開発対象ではないため不定`

- 外部機能層から参照される OS, Framework, Library など

## プログラムのフローと依存関係

このアーキテクチャは、ユーザーからみたプログラムの流れを中心に構築されています。
ここでユーザーとは、人間だけではなく、エージェントや他システムを含みます。

このアーキテクチャにおいて、プログラムのフローは各層の間を以下のように流れます。

`「焦点」（Focus） →「作用」（Interaction） →「接続」（Port） →「外部機能」（Operation / Storages / Reaction） `

また、各層は以下のように依存関係を持ちます。

`「焦点」（Focus） →「作用」（Interaction） →「接続」（Port） ←「外部機能」（Operation / Storages / Reaction）`

「外部機能」のみが「外部」に依存します。

`「外部機能」（Operation / Storages / Reaction） →「外部」（External Block）` 

「焦点構成部」は「焦点層」「接続層」「外部機能層」に依存します。

`「焦点構成部」（Focus Builder Block） → 「焦点層」（Focus）,「接続層」（Function Port）,「外部機能層」（Function External）` 

全ての Layer と Block は「関心」に依存します。（External Block を除く）

`All Layers and Blocks → 「関心」（Domain Block）` 

## 各層の定義名と名前空間

各層の定義は、プログラムの名前空間およびフォルダ、ディレクトリ構造と一致させることができます。

さらに、アルファベット順にソートした時に、おおむねプログラムのフローの順に各レイヤーが並びます。

これにより、構造とコードの対応関係を保ちながら、認知しやすくクリーンな設計を維持できます。

## 「焦点」と「作用」と「機能」

「焦点」（Focus）は、ユーザーにとって単一の意味を持つフローの単位であり、複数の「作用」により構成されます。

「作用」（Interaction）は、システム内部において単一の意味を持つフローの単位であり、複数の「機能」により構成されます。

「機能」（Operation, Reaction, Storage）は、作用を実現するための処理であり、「接続」（Function Port）によって抽象化され、「外部機能」（Function Externalにより実装されます。

### 中断についての違い

「機能」はキャンセルやエラー、例外による中断の動作がありますが、
「焦点」と「作用」には基本的に中断の動作がありません。

厳密には、状況に応じて、「正しく中断を終え、その事をユーザーに伝える」ことで「焦点」と「作用」は常に正常に終了します。
（詳細は「「焦点」と「作用」の推奨パターンとアンチパターン」で後述）

### 状態についての違い

「機能」は「管理」（Storage）に代表されるように、Mutable な状態（Memory State）を持つことができますが、「焦点」と「作用」の状態は Immutable である必要があります。

開始された「焦点」と「作用」で扱われる遷移状態は、
それら自身のスコープを抜けるまでに完全に破棄される必要があります。

### 「焦点」と「作用」の制約とアンチパターン

「焦点」は必ず以下の条件を満たす必要があります

- 「外部機能」はもちろん、「接続」にも依存しない
- ユーザーにとって単一の意味を持つ
- ユーザーにとって単一の目的を持つ
- 最後は必ず「明確な焦点の終了」を示す作用を実行する

また、ユーザーにとって主観的な目的を持たない「焦点」はアンチパターンです。  
ただし、単一の「作用」をラップする「焦点」は許可されます。 

「作用」は必ず以下の条件を満たす必要があります。

- 「外部機能」に依存しない
- システム内における単一の意味を持つ
- システム内における単一の目的を持つ
- 最後は必ず「明確な作用の終了」を示す反応を実行する

また、必要以上に巨大な「作用」はアンチパターンです。  

これを満たす範囲であれば、複雑な「作用」のフローを、1つの作用内で複数の入力や反応を利用することで実現出来ます。
その場合、UX的品質を担保するために、十分短い間隔での「反応」を含んだ上で、
アーキテクチャとしての制約として、作用の最後には必ず反応によってその作用の「明確な終了」を示す必要があります。

また「明確な終了」を示すという制約は、「焦点」のフローにおいても同様です。

最終的な「焦点」と「作用」の詳細な粒度はチームの意向に委ねられ、また粒度の計画的変更も許容されます。
