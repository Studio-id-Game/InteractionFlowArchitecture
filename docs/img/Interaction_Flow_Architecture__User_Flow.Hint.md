# Interaction Flow Architecture - User Flow

## ALT
Context の流れを示す図。Current Context から ProgramFlow Builder Block を経て ProgramFlow が実行され、Interaction Layer と Function Port / Function External Layer を通じて処理され、新しい Context が返る構造を表す。

## Visual Structure
- 左側に User の縦長パネルがある
- その右に Current Context からの開始矢印がある
- 中央左に ProgramFlow Builder Block があり、ここからフローを組み立てる
- 中央右に Layers 領域があり、ProgramFlow Layer, Interaction Layer, Function Port Layer, Function External Layer が縦に並ぶ
- 右側に Domain Block と External Block が配置される
- 下側に New Context があり、次のループへ戻る流れを示す

## Exact Labels
- `Current Context`
- `New Context`
- `User`
- `ProgramFlow Builder Block`
- `Layers`
- `ProgramFlow Layer`
- `Interaction Layer`
- `Function Port Layer`
- `Function External Layer`
- `Domain Block`
- `External Block`
- `Context の更新`
- `ユーザーフローの実行`
- `システムフローの実行`
- `外部機能の実行`

## Relationships
- Current Context が入力され、ProgramFlow Builder Block がフローを構築する
- ProgramFlow Layer がユーザー視点のフローを実行する
- Interaction Layer が内部処理を担う
- Function Port Layer が抽象境界になる
- Function External Layer が実処理を行う
- その結果として Context が更新され、New Context が生成される
- 矢印の色は時間の流れ、処理の実行、Context 更新の区別を示す

## Style Constraints
- 大きな矢印と広い余白で、流れの方向が一目で分かる構図を維持する
- ピンク系の流れ、青系の実行、オレンジ系の更新を保つ
- ボックスは薄いグレー背景と黒枠を維持する
- ラベルは太字の英語見出しと小さめの日本語説明で構成する

## AI Update Notes
- `Focus` という語は使わず、`ProgramFlow` に置き換える
- フローの起点は `ProgramFlow Builder Block` と `Current Context`
- `ProgramFlow` がユーザー視点の実行単位であることを明示する
- 時系列の流れを崩さず、Context 更新の循環構造を優先する
