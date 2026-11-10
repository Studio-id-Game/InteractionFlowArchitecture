# EscapeGameの名前空間と配置

Aspectsは関心→レイヤーの順に置く。現在の責務は、Viewが表示変換と入力、Gameplayがゲームの判断、
Applicationが入力と不変ゲーム状態の安全な共有、Sharedが契約・値・共通補助処理、Lifetimeが共通基底と全体停止である。

| 名前空間の末尾 | 要素 |
| --- | --- |
| Aspects.Gameplay.Entities | WorldRules |
| Aspects.Gameplay.ExternalPorts | IGameplayReaction |
| Aspects.Gameplay.Externals | GameplayReaction（更新結果検証を含む）、GameLifetimeReaction |
| Aspects.Gameplay.Interactions | BeginSession、BeginActionSelection、HandleGameInput、HandleControlInput、ApplyPlayerAction、ApplyEnemyAction、PresentResult、EndSession |
| Aspects.Gameplay.Entities.Contexts | GameplayContext |
| Aspects.View.Entities | FrameState、WindowState、PresentationClock |
| Aspects.View.ExternalPorts | IWindowOperation、IWindowReaction |
| Aspects.View.Externals | RaylibWindowOperation、RaylibWindowReaction、RaylibLayout |
| Aspects.View.Interactions | OpenWindow、PresentFrame、CloseWindow |
| Aspects.View.Entities.Contexts | ViewContext |
| Aspects.Application.Entities.Access | EscapeAccess（本体・Gameplay・View・Lifetimeの4ファイルとprivateなSessionRuntime） |
| Aspects.Application.Entities.Contexts | EscapeApplicationContext |
| Aspects.Application.Externals | GameInputOperation |
| Aspects.Lifetime.Entities.Contexts | LifetimeContext（抽象基底） |
| Aspects.Lifetime.Interactions | RequestShutdown |
| Aspects.Shared.Entities | GameState、InputFeedback、GameEvent、GameControlInput、RoomDefinition、CleanupInfo、FailureCollector、その他の共有値 |
| Aspects.Shared.Entities.Access | IGameplayAccess、IViewAccess、ILifetimeAccess |
| Aspects.Shared.Entities.Contexts | ILifetimeContext、IGameplayContext、IViewContext、IApplicationContext、ContextAccessValidation |
| Aspects.Shared.ExternalPorts | IGameInputOperation、IGameLifetimeReaction |
| Aspects.Shared.Externals | GameExceptionHandling、GameCancellationHandling |
| Aspects.各範囲.Builders | GameplayProfile、ViewProfile、ApplicationProfile、LifetimeProfile、SharedProfile |

各名前空間はInteractionFlow.Samples.EscapeGameを先頭とし、ディレクトリーも同じ順に揃える。
要素のない階層は作らない。
ルートのBuilders/EscapeGameProfileは各ProfileをApplyし、ゲーム進行のSystemFlowを自身で登録する。
SystemFlowsとProgramはルートのまま、全体合成と実行寿命を扱う。

## 依存と責務

Sharedは具体Aspectへ依存しない。各Aspectは自身とSharedを参照し、基底としてのLifetimeへの依存も許容する。
具体Context3型はLifetimeContextを継承し、Lifetimeの保持・取得・参照解放を共有する。
Context契約の所属にかかわらず、各機能は必要な最も狭いContextを受け取る。
全体を跨ぐ実行はルートのSystemFlowが合成する。

複数Aspectが利用するPortはSharedに置き、単一Aspectの機能契約と実装はそのAspectへまとめる。
IWindowReactionとCloseWindowはViewに所属し、Open・DrawFrame・Closeを同じRaylibWindowReactionが実装する。
CloseWindowの登録はViewProfile、全体停止要求の登録はLifetimeProfileが担当する。
Closeが必要とする引数は引き続きILifetimeContextであり、WindowStateとCleanupInfoはContextから取得する。
WorldRulesは外部環境を使わない通常の計算としてGameplay.Entitiesに置く。
Applicationはロック内で渡された同期更新関数を実行するだけで、WorldRulesやゲーム段階の条件を参照しない。
ゲーム状態からの文言・表示・演出指定の変換はViewに閉じる。
GameControlReaction / IGameControlReactionは廃止し、制御要求の解釈をGameplayReactionへまとめた。
HandleControlInputもGameplayへ所属し、Viewの入力を独立した制御経路で処理する。

既存Analyzerは名前空間中のExternalPorts / Externals / Interactions等を使い、従来のレイヤー規則を適用する。
Aspect間の上記配置方針に対する新しいAnalyzerは追加しない。

## 確認

単純なデータ共有モデルのビルド・テスト・実画面・計測結果は[サンプルREADME](../../InteractionFlow.Samples.EscapeGame/README.md)を参照する。
旧ルール注入・受付ポリシー・待機ID方式は撤回し、[ゲーム状態と入力の共有](./ESCAPE_GAME_GAMEPLAY_RULES.md)へ置き換えた。

### ルール分離前の確認記録

分類間依存の整理後、Shared以外のAspectを跨ぐ参照はLifetimeContextへの継承3件だけになった。
Shared自身から具体Aspectへの依存もない。今回の確認はimport・参照の点検であり、自動検査ルールは追加していない。
サンプルの名前空間・usingを除くコードトークンの集合は移動前と一致し、処理内容は変更していない。
全体・計測プロジェクトのビルドは警告・エラー0件、全570テストが成功した。

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
