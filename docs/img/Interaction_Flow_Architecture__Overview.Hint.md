# Interaction Flow Architecture - Overview

## ALT
System / Application / Service の全体図。左に User、中央に Layers、右に Blocks を配置し、ProgramFlow Layer と Interaction Layer、Function Port Layer、Function External Layer、ProgramFlow Builder Block、Domain Block、External Block の関係を示す。

## Visual Structure
- 左側に User の縦長パネルがある
- 中央の大きな枠に Layers が縦に4段並ぶ
- Layers は上から ProgramFlow Layer, Interaction Layer, Function Port Layer, Function External Layer
- 右側の Blocks 領域に ProgramFlow Builder Block, Domain Block, External Block が並ぶ
- 図全体は外枠で囲われ、各領域は点線の区画で分けられている

## Exact Labels
- `System / Application / Service`
- `User`
- `Layers`
- `ProgramFlow Layer`
- `Interaction Layer`
- `Function Port Layer`
- `Function External Layer`
- `Blocks`
- `ProgramFlow Builder Block`
- `Domain Block`
- `External Block`

## Relationships
- ProgramFlow Layer はユーザーの目的を表す上位フロー単位
- Interaction Layer は ProgramFlow を構成する内部フロー単位
- Function Port Layer は外部依存を抽象化する境界
- Function External Layer は実装を担う外部依存
- ProgramFlow Builder Block は ProgramFlow の構築と DI の注入を担う
- Domain Block は全レイヤーの前提となるデータ構造を表す
- External Block は OS, Framework, Library などの外部要素を表す

## Style Constraints
- 白背景、黒い枠線、薄いグレーのボックスを保つ
- 見出しは太字で大きく、説明文は小さめに置く
- 角が一部切れた図形のスタイルを維持する
- 日本語ラベルと英語ラベルの混在を保つ
- 余白と左右バランスは現行 SVG に合わせる

## AI Update Notes
- `Focus` という語は使わず、すべて `ProgramFlow` に置き換える
- レイヤー順とブロックの相対配置は変えない
- `ProgramFlow Builder Block` と `ProgramFlow Layer` を最優先で正確に維持する
- 説明文は概念説明よりも、図の構造を正確に伝えることを優先する
