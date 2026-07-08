# Interaction Flow Architecture - Dependency Diagram

## ALT
依存関係を示す図。中央に Layers、右に Domain Block、下に External Block、左に ProgramFlow Builder Block を置き、ProgramFlow Layer から Interaction Layer、Function Port Layer、Function External Layer へと依存が流れる構造を表す。

## Visual Structure
- 左側に ProgramFlow Builder Block があり、Layers に向かって点線矢印が伸びる
- 中央の Layers 領域は4段構造
- 上から ProgramFlow Layer, Interaction Layer, Function Port Layer, Function External Layer
- Interaction Layer 内に Interactions.Rules があり、Interaction を支える
- Function Port Layer の下に Function External Layer があり、ポートと実装が対応する
- 右側に Domain Block が独立して配置される
- 下側に External Block があり、Function External Layer から利用される

## Exact Labels
- `ProgramFlow Builder Block`
- `Layers`
- `ProgramFlow Layer`
- `Interaction Layer`
- `Function Port Layer`
- `Function External Layer`
- `Domain Block`
- `External Block`
- `ProgramFlow`
- `Interaction`
- `Interactions.Rules`
- `OperationPort`
- `ReactionPort`
- `SilentExternalPort`
- `StoragePort`
- `Operation`
- `Reaction`
- `SilentExternal`
- `Storage`
- `Entities`
- `Entities.Rules`

## Relationships
- ProgramFlow Layer は Interaction Layer を利用する
- Interaction Layer は Function Port Layer に依存する
- Function External Layer は Function Port Layer の抽象を実装する
- Domain Block は全レイヤーから参照される
- External Block は Function External Layer の実装基盤になる
- ProgramFlow Builder Block は ProgramFlow と依存オブジェクトを組み立てる
- 破線矢印は依存関係または注入関係を示す

## Style Constraints
- 依存の流れが見えるように、点線矢印を明確に保つ
- 各レイヤーの箱は薄いグレーの背景と黒枠を維持する
- 配置は左から右、上から下へ読みやすい構成を保つ
- 見出しのフォントと箱内ラベルの太さを揃える
- 日本語の補足ラベルは小さめに置く

## AI Update Notes
- `Focus` という語は使わず、`ProgramFlow` に置き換える
- `ProgramFlow Builder Block` の役割説明は「構築」と「DI 注入」を中心に書く
- `Interaction` と `Function` の役割分担が読み取れるようにする
- コード実態に合わせて、旧名ではなく現行名で説明する
