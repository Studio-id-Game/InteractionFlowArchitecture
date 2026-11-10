# EscapeGameの名前空間と配置

ExternalPorts / Externals / Interactionsは、Aspects配下でContext契約の範囲を先、レイヤーを後に配置する。
「依存を完全に含む最も狭い範囲へ所属する」を共通ルールとする。
SystemFlows、Entities、Buildersの配置は今回変更しない。

| 範囲 | 所属条件 |
| --- | --- |
| Lifetime | ILifetimeContextだけを必要とする |
| Gameplay | IGameplayContextで足りる。Lifetimeも含む |
| View | IGameViewContextで足りる。Lifetimeも含む |
| Application | GameplayとViewの両方が必要で、どちらか一方には収まらない |

Lifetimeは両用途の基底、Applicationは集約である。
例: InteractionのEndSessionはGameplay、RequestShutdownとCloseWindowはLifetime。
GameplayとLifetimeの和はGameplay、ViewとLifetimeの和はViewになる。
型名が複数あるだけではApplicationへ広げない。GameplayとViewの和だけがApplicationになる。
IGameLifetimeReaction / GameLifetimeReactionはGameplay、IWindowReaction / RaylibWindowReactionはViewへ置く。

## 配置

| 名前空間の末尾 | 要素 |
| --- | --- |
| Aspects.Gameplay.ExternalPorts | IGameplayReaction、IGameLifetimeReaction |
| Aspects.Gameplay.Externals | GameplayReaction、GameLifetimeReaction、GameUpdateValidation |
| Aspects.Gameplay.Interactions | BeginSession、BeginActionSelection、HandleGameInput、ApplyPlayerAction、ApplyEnemyAction、PresentResult、EndSession |
| Aspects.View.ExternalPorts | IWindowOperation、IGameControlReaction、IWindowReaction |
| Aspects.View.Externals | RaylibWindowOperation、RaylibWindowReaction、GameControlReaction、RaylibLayout |
| Aspects.View.Interactions | OpenWindow、PresentFrame、HandleControlInput |
| Aspects.Lifetime.Interactions | RequestShutdown、CloseWindow |
| Aspects.Application.ExternalPorts | IGameInputOperation |
| Aspects.Application.Externals | GameInputOperation |
| Externals（既存位置） | GameExceptionHandling、GameCancellationHandling |

各名前空間はInteractionFlow.Samples.EscapeGameを先頭とする。ディレクトリーも同じ順に揃える。
Aspectsは範囲別の機能をまとめる配置上の階層であり、新しい依存範囲やレイヤーではない。
SystemFlows、Entities、Buildersと分類外の共通HandlerはAspectsへ移さず、既存位置に置く。
要素のないレイヤーは作らない。複数範囲を扱う既存Port / Externalを、配置のためだけに分割しない。
専用ヘルパーは補助対象に従い、GameUpdateValidationはGameplay、RaylibLayoutはViewへ置く。
IFlowContextを受け取る共通例外・取消Handlerは、特定のContext契約を入口にしない共通処理として既存位置に残す。

## 依存の意味

この分類はContextの範囲と役割を示し、役割間の参照を一方向に限定するものではない。
Aspects.Gameplay.Interactions.HandleGameInputはAspects.Application.ExternalPorts.IGameInputOperationを使用する。
Aspects.View.Interactions.PresentFrameもAspects.Application.ExternalPorts.IGameInputOperationを使用する。
利用する入口のContext範囲で判定し、集約されたPortの名前空間をそのまま呼び出し元の必要範囲とはしない。
Aspects.View.Interactions.OpenWindowはAspects.View.ExternalPorts.IWindowReactionを使用する。
包含関係を、そのままプロジェクトやレイヤーの参照方向と解釈しない。

既存Analyzerは名前空間の各要素からExternalPorts / Externals / Interactions等を探すため、
今回の順序でも従来のレイヤー依存規則が適用される。Analyzerのコード・設定は変更しない。
この確認は役割間の参照制約を新たに保証するものではない。

## 確認

Aspects階層の追加後は、構成説明・配置表・ソースへのリンクを更新した。
テスト・計測コードに残っていた旧名前空間のusingも更新した。
全体と計測プロジェクトのビルドは警告・エラー0件、全569テストが成功した。

### Aspects追加前の確認記録

26型を移動し、Builder・SystemFlow・テスト・計測コードと文書リンクを更新した。
その後、全28要素（26型と共通Handler2型）を包含関係で再点検した。
Lifetimeを基底として展開すると、WindowReaction系2型とGameLifetimeReaction系2型がApplicationでは広すぎたため再配置した。
他の型・専用ヘルパー・分類外Handlerには同じ誤りはなかった。
メソッド本体・契約・ゲーム規則・描画・Cancellation共有・状態の同期境界は変更していない。
名前空間・using・参照名を除いたC#66ファイルのトークン比較で差分なし。
全体ビルドは警告・エラー0件、EscapeGame99件と他プロジェクト470件、計569件が成功。
今回の4型の再配置後も全体ビルドと全569件が成功した。
初回整理時のRelease簡易計測では、通常PresentFrameは488B/回・約221〜234nsで、割り当て量を維持している。今回の再配置では計測を繰り返していない。
実画面での再確認は未実施。サブエージェントは使用していない。

関連: [ContextとAccessの分類](./ESCAPE_GAME_ACCESS_CONTRACT_REVIEW_PLAN.md)、
[サンプルの構成](../../InteractionFlow.Samples.EscapeGame/README.md)。
