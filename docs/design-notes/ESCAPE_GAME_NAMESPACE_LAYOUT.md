# EscapeGameの名前空間と配置

ExternalPorts / Externals / Interactionsは、Aspects配下でContext契約の範囲を先、レイヤーを後に配置する。
「依存を完全に含む最も狭い範囲へ所属する」を共通ルールとする。
SystemFlowsとBuildersは既存位置に置き、共有EntitiesはAspects.Sharedへまとめる。View専用の状態はAspects.View.Entitiesへ置く。

| 範囲 | 所属条件 |
| --- | --- |
| Lifetime | ILifetimeContextだけを必要とする |
| Gameplay | IGameplayContextで足りる。Lifetimeも含む |
| View | IViewContextで足りる。Lifetimeも含む |
| Application | GameplayとViewを集約・接続し、共有状態を管理する機能・実装 |
| Shared | Aspect間で共有する契約・値・共通補助処理 |

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
| Aspects.Gameplay.Entities | WorldRules、ActionEffect、PlayerState、InventoryState、MechanismState、EnemyState、SessionProgressState |
| Aspects.View.ExternalPorts | IWindowOperation、IGameControlReaction、IWindowReaction |
| Aspects.View.Externals | RaylibWindowOperation、RaylibWindowReaction、GameControlReaction、RaylibLayout |
| Aspects.View.Interactions | OpenWindow、PresentFrame、HandleControlInput |
| Aspects.View.Entities | FrameState、WindowState |
| Aspects.Lifetime.Interactions | RequestShutdown、CloseWindow |
| Aspects.Application.ExternalPorts | IGameInputOperation |
| Aspects.Application.Externals | GameInputOperation |
| Aspects.Shared.Entities | RoomDefinition、CleanupKind、CleanupInfo、FailureCollector、GameValues.csのゲーム値型、ExecutionMode、PresentationValues.csの提示値型、ViewSnapshot |
| Aspects.Gameplay.Entities.Contexts | GameplayContext |
| Aspects.View.Entities.Contexts | ViewContext |
| Aspects.Lifetime.Entities.Contexts | LifetimeContext（抽象基底） |
| Aspects.Application.Entities.Contexts | EscapeApplicationContext |
| Aspects.Shared.Entities.Access | IGameplayAccess、IViewAccess、ILifetimeAccess |
| Aspects.Shared.Entities.Contexts | ILifetimeContext、IGameplayContext、IViewContext、IApplicationContext、ContextAccessValidation |
| Aspects.Application.Entities.Access | EscapeAccess（4つのpartialファイルとprivateなSessionRuntime・DisplayState・PresentationRuntime） |
| Aspects.Shared.Externals | GameExceptionHandling、GameCancellationHandling |
| Aspects.Gameplay.Builders | GameplayProfile（Reaction・Interactionを登録） |
| Aspects.View.Builders | ViewProfile |
| Aspects.Lifetime.Builders | LifetimeProfile |
| Aspects.Application.Builders | ApplicationProfile（入力接続を登録） |
| Aspects.Shared.Builders | SharedProfile（Shared.Externalsの共通Handlerを登録） |

各名前空間はInteractionFlow.Samples.EscapeGameを先頭とする。ディレクトリーも同じ順に揃える。
Aspectsは範囲別の機能をまとめる配置上の階層であり、新しい依存範囲やレイヤーではない。
SystemFlowsと全体合成用Buildersは既存位置に置く。ルートのEntitiesはShared.Entities、共通HandlerはShared.Externalsへ移す。
ルートBuildersには全体合成のEscapeGameProfileを残し、各Aspectの登録はその配下のBuildersへ分ける。
EscapeGameProfileはShared・Application・Gameplay・View・Lifetimeの各ProfileをApplyする。
ゲーム進行の3つのSystemFlowは全体構成としてEscapeGameProfile自身が登録する。
各Profileは他Aspectを再適用せず、自身の登録だけを担当する。全体合成後に依存を解決し、従来どおり後続登録でPortを差し替えられる。
FrameStateとWindowStateは画面側で扱う状態としてAspects.View.Entitiesへまとめる。
ゲーム規則と内部の論理状態7型はAspects.Gameplay.Entitiesへ置く。
これらはApplicationのEscapeAccessとSessionRuntimeから利用する。
Context契約は共有の引数契約としてShared.Entities.Contextsへ置き、具体Contextは対応する範囲のEntities.Contextsへ置く。
具体Context3型はLifetimeContextを継承する。基底はLifetimeの保持・破棄確認付き取得・型探索と参照解放だけを共有し、Accessの所有権は取得しない。
派生ContextのDisposeCoreは固有参照を解放してからbase.DisposeCoreを呼ぶ。役割別の探索制限と追加値検証は派生側に残す。
契約の配置にかかわらず、各機能は必要な依存を含む最も狭いContext契約を受け取る。
3つのAccess契約は共有状態への操作契約としてShared.Entities.Accessへまとめる。
全Access契約を実装するEscapeAccessは、Contextが提供する値としてApplication.Entities.Accessへ置く。
partialは本体と、IGameplayAccess・IViewAccess・ILifetimeAccessに対応するGameplay・View・Lifetimeの4ファイルとする。
共有フィールド・補助メソッド・内部クラス3型は本体へ集約する。
GetSessionProgressはGameplayとViewの両契約に共通するため、本体に一つの実装を置く。
Aspect間の共有契約・値はSharedへ置き、集約・接続・共有状態管理の実装はApplicationへ置く。
旧GameValues.csの型はすべてAspects.Shared.Entitiesへ移し、GameValues.cs、ExecutionMode.cs、PresentationValues.cs、ViewSnapshot.csへ分ける。
RoomDefinition・終了情報・失敗収集はShared.Entities、Contextの共通検証処理はShared.Entities.Contextsへ置く。
要素のないレイヤーは作らない。複数範囲を扱う既存Port / Externalを、配置のためだけに分割しない。
専用ヘルパーは補助対象に従い、GameUpdateValidationはGameplay、RaylibLayoutはViewへ置く。
IFlowContextを受け取る共通例外・取消Handlerは、特定のContext契約を入口にしない共通処理としてShared.Externalsへ置く。

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
