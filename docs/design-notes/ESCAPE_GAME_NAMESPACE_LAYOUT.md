# EscapeGameの名前空間と配置

Aspectsは関心→レイヤーの順に置く。現在の責務は、Viewが表示変換と入力、Gameplayがゲームの判断、
Applicationが入力と不変ゲーム状態の安全な共有、Sharedが共有契約・値、Lifetimeが共通基底と停止・終了方針である。

| 名前空間の末尾 | 要素 |
| --- | --- |
| Aspects.Gameplay.Entities | WorldRules |
| Aspects.Gameplay.ExternalPorts | IGameplayReaction、IGameInputOperation、IGameSessionReaction |
| Aspects.Gameplay.Externals | GameplayReaction（更新結果検証を含む）、GameInputOperation、GameSessionReaction |
| Aspects.Gameplay.Interactions | BeginSession、PlayTurn、HandleResult、HandleControlInput、EndSession、GameplayCheckpoint（内部補助） |
| Aspects.Gameplay.Entities.Contexts | GameplayContext |
| Aspects.View.Entities | FrameState、WindowState、PresentationClock |
| Aspects.View.ExternalPorts | IWindowOperation、IWindowReaction、IGameInputSubmissionOperation |
| Aspects.View.Externals | RaylibWindowOperation、RaylibWindowReaction、GameInputSubmissionOperation、RaylibLayout |
| Aspects.View.Interactions | OpenWindow、PresentFrame、CloseWindow |
| Aspects.View.Entities.Contexts | ViewContext |
| Aspects.Application.Entities | SessionState、SessionRuntime、InputWait、ResumeWait、PresentationWait（すべてinternal） |
| Aspects.Application.Entities.Access | EscapeAccess（本体・Gameplay・View・Lifetimeの4ファイル） |
| Aspects.Application.Entities.Contexts | EscapeApplicationContext |
| Aspects.Lifetime.Entities | CleanupInfo、CleanupKind、FailureCollector |
| Aspects.Lifetime.Entities.Contexts | LifetimeContext（抽象基底）、ContextAccessValidation |
| Aspects.Lifetime.ExternalPorts | IShutdownReaction |
| Aspects.Lifetime.Externals | ShutdownReaction、GameExceptionHandling、GameCancellationHandling |
| Aspects.Lifetime.Interactions | RequestShutdown |
| Aspects.Shared.Entities | GameInfo<T> |
| Aspects.Shared.Entities.Game | GameState、GameStage、GameOutcome、RoomDefinition、GridPosition、GameAction、ActionKind、GameCommand |
| Aspects.Shared.Entities.Session | GameChangeInfo、GameViewInfo、GameUpdateResult、GameUpdateStatus、ExecutionMode、InputFeedback、GameControlInput、GameControlCommand、GameEvent、GameEventKind、PresentationKey、PresentationCompletion |
| Aspects.Shared.Entities.Access | IGameplayAccess、IViewAccess、ILifetimeAccess |
| Aspects.Shared.Entities.Contexts | ILifetimeContext、IGameplayContext、IViewContext、IApplicationContext |
| Aspects.各範囲.Builders | GameplayProfile、ViewProfile、ApplicationProfile、LifetimeProfile、SharedProfile |

各名前空間はInteractionFlow.Samples.EscapeGameを先頭とし、ディレクトリーも同じ順に揃える。
要素のない階層は作らない。
Gameはゲーム内の状態・位置・行動、Sessionは共有アクセスを通した制御・提示・反映情報を置く。
関連する列挙型は主となるデータ型と同じファイルにまとめ、GameValues / PresentationValuesの混在を解消する。
ルートのBuilders/EscapeGameProfileは各ProfileをApplyし、ゲーム進行のSystemFlowを自身で登録する。
SystemFlowsとProgramはルートのまま、全体合成と実行寿命を扱う。

## 依存と責務

Sharedは具体Aspectへ依存しない。各Aspectは自身とSharedを参照し、基底としてのLifetimeへの依存も許容する。
具体Context3型はLifetimeContextを継承し、Lifetimeの保持・取得・参照解放を共有する。
Context契約の所属にかかわらず、各機能は必要な最も狭いContextを受け取る。
全体を跨ぐ実行はルートのSystemFlowが合成する。
セッション内のターン反復と結果確認はGameSessionSystemFlow、1ターン内のOperation / Reaction接続はPlayTurn、
結果提示からリトライまでの接続はHandleResultが担う。Interaction同士は直接実行しない。
GameplayCheckpointはGameplay.Interactionsに置き、段階間とフロー間で継続方針を共有する。

複数Aspectが利用するPortはSharedに置き、単一Aspectの機能契約と実装はそのAspectへまとめる。
IWindowReactionとCloseWindowはViewに所属し、Open・DrawFrame・Closeを同じRaylibWindowReactionが実装する。
入力待機はGameplayのIGameInputOperation / GameInputOperation、入力配送はViewのIGameInputSubmissionOperation / GameInputSubmissionOperationに分ける。
セッション終了はGameplayのIGameSessionReaction / GameSessionReaction、全体停止はLifetimeのIShutdownReaction / ShutdownReactionが担当する。
各Portと実装は自身のAspectのProfileで登録する。ApplicationProfileとSharedProfileも構成の入口として維持するが、現在の登録対象はない。
Shared.ExternalPortsは不要になり、共有する状態・Access・Context契約はShared.Entitiesに残す。
Closeが必要とする引数は引き続きILifetimeContextであり、WindowStateとCleanupInfoはContextから取得する。
CleanupInfoとFailureCollector、例外・キャンセルHandlerは、停止要求と終了結果の確定を担うためLifetimeに置く。
Handlerの登録もLifetimeProfileへまとめ、SharedからLifetimeへの依存を作らない。
ContextAccessValidationは唯一の呼び出し元であるLifetimeContextと同じ名前空間に置く。
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

現在の契約とテストの対応、直近の検証結果は[テスト点検](./ESCAPE_GAME_TEST_COVERAGE.md)を参照する。

過去の分類・移行時の確認記録は[アーカイブ](../../archives/escape-game/README.md)を参照する。
