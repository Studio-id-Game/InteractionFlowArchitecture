# EscapeGameの名前空間と配置

ExternalPorts / Externals / Interactionsは、Context契約の範囲を先、レイヤーを後に配置する。
「依存を完全に含む最も狭い範囲へ所属する」を共通ルールとする。
SystemFlows、Entities、Buildersの配置は今回変更しない。

| 範囲 | 所属条件 |
| --- | --- |
| Lifetime | ILifetimeContextだけを必要とする |
| Gameplay | IGameplayContextで足りる。Lifetimeも含む |
| View | IGameViewContextで足りる。Lifetimeも含む |
| Application | Gameplay / View、または各範囲にまたがる複数の入口をまとめる |

Lifetimeは両用途の基底、Applicationは集約である。
例: InteractionのEndSessionはGameplay、RequestShutdownとCloseWindowはLifetime。
両操作をまとめるIGameLifetimeReactionとGameLifetimeReactionはApplicationへ置く。

## 配置

| 名前空間の末尾 | 要素 |
| --- | --- |
| Gameplay.ExternalPorts | IGameplayReaction |
| Gameplay.Externals | GameplayReaction、GameUpdateValidation |
| Gameplay.Interactions | BeginSession、BeginActionSelection、HandleGameInput、ApplyPlayerAction、ApplyEnemyAction、PresentResult、EndSession |
| View.ExternalPorts | IWindowOperation、IGameControlReaction |
| View.Externals | RaylibWindowOperation、GameControlReaction、RaylibLayout |
| View.Interactions | OpenWindow、PresentFrame、HandleControlInput |
| Lifetime.Interactions | RequestShutdown、CloseWindow |
| Application.ExternalPorts | IGameInputOperation、IGameLifetimeReaction、IWindowReaction |
| Application.Externals | GameInputOperation、GameLifetimeReaction、RaylibWindowReaction |
| Externals（既存位置） | GameExceptionHandling、GameCancellationHandling |

各名前空間はInteractionFlow.Samples.EscapeGameを先頭とする。ディレクトリーも同じ順に揃える。
要素のないレイヤーは作らない。複数範囲を扱う既存Port / Externalを、配置のためだけに分割しない。
専用ヘルパーは補助対象に従い、GameUpdateValidationはGameplay、RaylibLayoutはViewへ置く。
IFlowContextを受け取る共通例外・取消Handlerは、特定のContext契約を入口にしない共通処理として既存位置に残す。

## 依存の意味

この分類はContextの範囲と役割を示し、役割間の参照を一方向に限定するものではない。
Gameplay.Interactions.HandleGameInputはApplication.ExternalPorts.IGameInputOperationを使用する。
View.Interactions.OpenWindowはApplication.ExternalPorts.IWindowReactionを使用する。
Application.Externals.RaylibWindowReactionはView.Externals.RaylibLayoutを使用する。
包含関係を、そのままプロジェクトやレイヤーの参照方向と解釈しない。

既存Analyzerは名前空間の各要素からExternalPorts / Externals / Interactions等を探すため、
今回の順序でも従来のレイヤー依存規則が適用される。Analyzerのコード・設定は変更しない。
この確認は役割間の参照制約を新たに保証するものではない。

## 確認

26型を移動し、Builder・SystemFlow・テスト・計測コードと文書リンクを更新した。
メソッド本体・契約・ゲーム規則・描画・Cancellation共有・状態の同期境界は変更していない。
名前空間・using・参照名を除いたC#66ファイルのトークン比較で差分なし。
全体ビルドは警告・エラー0件、EscapeGame99件と他プロジェクト470件、計569件が成功。
最終のusing整理後も全体ビルドと関連99件が成功した。
Release簡易計測の通常PresentFrameは488B/回・約221〜234nsで、割り当て量を維持している。
実画面での再確認は未実施。サブエージェントは使用していない。

関連: [ContextとAccessの分類](./ESCAPE_GAME_ACCESS_CONTRACT_REVIEW_PLAN.md)、
[サンプルの構成](../../InteractionFlow.Samples.EscapeGame/README.md)。
