# EscapeGame Access契約の再分類プラン

状態: 3Accessへの再編と、後半のContext契約分割を実装済み。
本文は再分類・分割時点の名称を記録している。現行名称との対応は[命名整理](./ESCAPE_GAME_NAMING_PLAN.md)を参照。
調査・実装ともサブエージェントは使用していない。
調査対象は単一EscapeApplicationContextへ戻した40af6fe時点の本番コードと、関連テスト・計測コード。
Core API、Context型、State、同期境界を変更せず、公開Access契約の分類を先に見直す。

実装結果: Gameplay / View / Lifetimeの3契約へ移行し、旧7契約とActiveSessionIdを削除した。
待機メソッド名をWaitForPresentationAsyncへ変更し、CreateSessionをGameplay契約へ公開した。
3Access再編の段階では単一EscapeApplicationContextと既存の生成メソッドを維持し、ノード・PortのContext引数型を変更しなかった。
既存94件にLifetime専用Handler2件とセッション割り当て1件を追加し、EscapeGame97件が成功した。
全体ビルドは警告・エラー0件、他プロジェクト470件を含む全567件が成功。
Release簡易計測は通常PresentFrameが488B/回・約227～238nsで、割り当て量を維持した。
実画面での再確認は未実施。処理手順と同期境界を変えず、非ネイティブ実行で検証した。

後続のContext分割も完了した。4つのContextインターフェースを実行経路の引数へ使い、
EscapeApplicationContext / GameplayContext / GameViewContextの3具体型とコンストラクターで構成している。
旧生成メソッドを削除し、具体Contextへのキャストなしで構築する。
全体ビルドは警告・エラー0件、EscapeGame99件と他プロジェクト470件、計569件が成功。
探索範囲・固定値検証・親探索と取消の分離・破棄・実フローへの限定Context受け渡しを確認した。
Release簡易計測の通常PresentFrameは488B/回・約231～239ns。実画面の再確認は未実施。

## 結論

現在の7契約を、ゲーム実行・画面接続・アプリ寿命の3契約へ再編する案を推奨する。
命名候補はIGameplayAccess / IGameViewAccess / IApplicationLifetimeAccess。
内部のEscapeAccessと一つのgateは共有したままにする。

目標は、契約数の最小化だけでなく、利用側が必要な協調操作を少ない契約から取得できること。
Stateの分割、Functionの分類、Accessの公開境界、ロックの境界は別々に判断する。
今回の分類変更だけで、具体Contextを受け取る各Functionから別Accessへの参照を禁止するわけではない。
Contextインターフェースや別Context型を同時に増やさず、分類の効果を先に確認する。

## 現在の呼び出し

本番コードの呼び出しを、メソッドの所在ではなく実行上の役割で追った。

| 呼び出し元 | 現在使っているAccessと操作 | 読み取れる役割 |
| --- | --- | --- |
| GameOperation.ReadAsync | Input.ReadAsync | ゲーム進行側が一件の入力を待つ |
| GameOperation.Submit | InputWriter.TrySubmit | 画面側が入力を受理先へ送る |
| GameReaction.BeginAsync | Session.BeginSession、Presentation.WaitAsync、Control.WaitForActiveAsync | 初期提示の開始と完了待機 |
| GameReaction.SelectAsync / EnableSelectionAsync / EnableResultInputAsync | Control.WaitForActiveAsync、Worldの対応更新 | 実行可能性を確認し、同じ更新をDeferred後も保持する |
| GameReaction.ApplyPlayerAsync / ApplyEnemyAsync / ShowResultAsync | Control.WaitForActiveAsync、Worldの対応更新、Presentation.WaitAsync | 論理更新と、その提示完了までの応答 |
| GameFlowCheckpoint | Session.GetProgress / ShutdownRequested、Control.WaitForActiveAsync | フローの停止・再開判断 |
| TurnSystemFlow / SessionSystemFlow | Session.GetProgress | 確定・勝敗・リトライから次の相互作用を選ぶ |
| GameSystemFlow | Session.GetProgress / ShutdownRequested、Contextのセッション生成 | セッションの反復と終了方針 |
| RaylibWindowOperation.Poll | Display.Snapshot | 操作可能性と提示対象を読み、FrameStateへ入力を記録する |
| RaylibWindowReaction.Draw | Display.Snapshot、Presentation.Advance / TryComplete | 一貫した表示を読み、描画後に提示完了を通知する |
| GameControlReaction.Change | Session.GetProgress、Control.TrySetPaused、Presentation.TrySkip | 画面操作によるポーズ・再開・省略要求 |
| SessionReaction.EndSession | Session.EndSession | 一つのセッションの待機と寿命を閉じる |
| SessionReaction.RequestShutdown | Session.RequestShutdown | アプリ全体の待機を閉じる |
| GameExceptionHandling / GameCancellationHandling | GetRequired&lt;IGameSessionAccess&gt;().RequestShutdown | Contextの具体型に依存せず停止を要求する |
| WindowSystemFlow | Session.ShutdownRequested | ゲーム実行の終了と全体停止を観測し、合流・Closeへ進む |
| EscapeApplicationContext.CreateSessionContext | EscapeAccess.CreateSession（internal） | IDと初期状態を割り当てる。公開・提示開始はReactionに残す |

RaylibWindowReaction.OpenはDrawへ接続する。Close自身はWindowStateと先行Resultを扱い、Access操作は呼ばない。
FrameState / WindowState / Finishing / CancellationはAccessの再分類対象ではない。

## 現在の分類が合わない点

- WorldとControlの再開待機、Presentationの完了待機はGameReaction内で繰り返し協調して使われる。
- Controlは「待つ」と「変更する」、Presentationは「待つ」と「進めて通知する」を同じ契約へ混在させている。
- Sessionはゲーム進行の参照・更新と、全体停止の共通経路を兼ねている。
- InputとInputWriterは方向こそ分かれるが、各一操作の独立契約である必要性は現サンプルでは確認できない。
- Displayは描画だけでなく入力可能性の判断にも使われるため、画面接続全体とまとめる方が利用側に合う。

現在は7インターフェース、計21メンバー。内部CreateSessionはこの数に含めない。
ActiveSessionIdは本番の利用側と計測コードから参照されず、ContextTestsが使用している。

## 推奨する3契約と全操作の配置

契約は役割別の表面とし、既存7契約を継承するだけの別名にはしない。
EscapeAccessが3契約を直接実装し、旧7契約は移行完了後に削除する。

| 新契約 | メンバー | 現在の所在 |
| --- | --- | --- |
| IGameplayAccess | ReadAsync | InputReader |
| IGameplayAccess | CreateSession | 現在internalのEscapeAccess.CreateSession。後述のContext契約の判断で公開対象へ追加 |
| IGameplayAccess | BeginSession / EndSession | Session |
| IGameplayAccess | GetProgress | Session |
| IGameplayAccess | ApplySelection / ApplyPlayer / ApplyEnemy | World |
| IGameplayAccess | EnterSelecting / ShowResult / EnableResultInput | World |
| IGameplayAccess | WaitForActiveAsync | Control |
| IGameplayAccess | WaitForPresentationAsync | Presentation.WaitAsyncを用途が分かる名前へ変更 |
| IGameViewAccess | Snapshot | Display |
| IGameViewAccess | TrySubmit | InputWriter |
| IGameViewAccess | Advance / TryComplete / TrySkip | Presentation |
| IGameViewAccess | TrySetPaused | Control |
| IGameViewAccess | GetProgress | Session。ポーズ変更時の現在値参照 |
| IApplicationLifetimeAccess | ShutdownRequested / RequestShutdown | Session |

ゲーム実行側13メンバー、画面接続側7メンバー、全体寿命2メンバー。
GetProgressは両方へ公開するが、実装とロック取得は一つのまま。
同じデータの参照を共有するためだけの4つ目のインターフェースは作らない。
ActiveSessionIdは公開契約から削除する候補とする。テストの観測はSnapshot.SessionIdで代替できる。
セッションIDの固定性を確認するテスト自体は維持する。
CreateSessionはIDと初期状態を割り当てるだけで、公開・提示開始はBeginSessionを呼ぶReactionに残す。
初案ではinternal維持としたが、後述のContext契約を具体型の生成メソッドなしで使うためGameplayへ含める判断とした。

### 進行参照を画面側にも残す理由

GameControlReactionはGetProgress(frame.SessionId).Pausedを読み、逆の値をTrySetPausedへ渡している。
Snapshotの再取得による置換は余分なスナップショット生成と対象ID確認を必要とする。
TryTogglePausedの新設なら一操作にできるが、ロック内の挙動まで変更する別の改善になる。
初回はGetProgressを共有して既存の操作手順を保ち、トグルAPIは必要性を別途判断する。

### 全体寿命を独立させる理由

ShutdownRequestedはWindow側とゲーム側の双方が参照する。
RequestShutdownは通常停止に加え、具体Contextを知らない共通Handlerから使用する。
二つを狭い共通契約にまとめると、Handlerがゲーム更新や画面操作の契約を要求せずに済む。
セッションの開始・終了とは対象と影響範囲が違うため、BeginSession / EndSessionはゲーム実行側へ置く。

## 代替案との比較

| 案 | 利点 | 残る問題 | 判断 |
| --- | --- | --- | --- |
| 全操作を1契約 | 最少の宣言、取得先も一つ | 更新・描画・通知・待機・停止の区別を契約から読めない | 推奨しない |
| ゲーム / 画面の2契約 | 入力・提示・制御の方向を分けられる | 全体寿命を片側へ寄せるか重複させ、Handlerも片側の広い契約へ依存する | 比較候補だが3契約を優先 |
| 推奨3契約 | 通常処理は役割ごとに一契約、共通停止だけ狭く独立 | GetProgressは両側に残る。ゲーム実行契約は12メンバーになる | 最少に近い分割で利用役割を表せる |
| 現行7契約 | 小さな関心別に名前がある | 呼び出し側で協調操作を毎回組み合わせ、複数契約が双方の役割を含む | 再編する |
| 待機・通知・読取等をさらに細分化 | メソッド単位の制約を作りやすい | 契約数とContext合成が増え、今回の目的と逆方向 | 見送る |

契約の独立した差し替え需要は、現在の本番構成からは確認できない。
テストはFunction Portを差し替え、Access自体は実際のEscapeAccessを使う構成が中心である。
Stateや提示・入力の内部ヘルパーは、公開契約を統合しても分割を維持できる。

## 利用側の到達形

単一EscapeApplicationContextへGameplay / View / Lifetimeの3プロパティを置く。
プロパティ・TryGetCore・固定追加値の検証は同じ3契約を対象にする。
各プロパティは同じEscapeAccess実体を返し、ラッパー・追加ロック・追加スナップショットを作らない。

| 利用箇所 | 主な取得先 |
| --- | --- |
| GameOperation.ReadAsync、GameReaction、EndSession | Gameplay |
| GameOperation.Submit、RaylibWindowOperation / Reaction、GameControlReaction | View |
| Turn / SessionSystemFlow | 直接はGameplay。GameFlowCheckpointを含めるとGameplayとLifetime |
| GameSystemFlow / GameFlowCheckpoint | GameplayとLifetime |
| WindowSystemFlow、RequestShutdown、共通Handler | Lifetime |

Reactionによる終了生成、SystemFlowによる合成・停止・合流、Contextの値提供という分担は変えない。
AccessへReactionEnd生成やTask所有、フロー選択を移さない。
GetRequiredは共通Handler等で使い、通常処理は3プロパティから直接取得する。

例としてApplyPlayerAsyncは、Control / World / Presentationの3取得先からGameplay一つへ変わる。
同じ待機・更新の順序を保ち、複数の呼び出しを自動実行する新しいAccessメソッドにはまとめない。

```csharp
var game = context.Gameplay;
while (await game.WaitForActiveAsync(context.SessionId, token))
{
    var change = game.ApplyPlayer(context.SessionId);
    // Deferred / Stopped / 不成立の判定は現在のReactionに残す。
    // 成功時はgame.WaitForPresentationAsyncで待ち、Reactionが終了結果を生成する。
}
```

GameControlReactionもSession / Control / Presentationの3取得先からView一つへ変わる。
RaylibWindowReaction.DrawはDisplay / Presentationの2取得先からView一つへ変わる。
GameFlowCheckpointはSession / ControlからGameplay / Lifetimeとなり、取得先数は減らないが
進行の参照・待機と、全体停止の観測という役割が分かれる。

### SystemFlow / Interactionから見た依存

上表はAccessの利用役割を示すもので、各ノードの依存宣言をAccessへ置き換えるものではない。
SystemFlowの依存ノードは引き続きInteraction / SystemFlow、Interactionの依存ノードはFunction Portとする。
同じ具体Contextを使う現段階では全Accessプロパティが見えるため、表の依存は型による利用禁止ではない。

| SystemFlow | 自身と共有補助が参照するAccess | 子の実行で必要な通常経路 |
| --- | --- | --- |
| WindowSystemFlow | Lifetime。ほかにContext生成・Cancellation・FrameState・結果集約 | View、Gameplay、Lifetime |
| GameSystemFlow | Gameplay、Lifetime。ほかにContext生成・Cancellation・Finishing | Gameplay、Lifetime |
| SessionSystemFlow / TurnSystemFlow | 直接はGameplay、Checkpoint経由でLifetimeも必要 | Gameplay、Lifetime |

Interaction本体の多くはAccessを直接操作せず、Function Portを呼ぶ。
以下はFunctionまで含めてInteractionの実行に必要なAccessである。

| Interaction | 通常経路のAccess | Access以外の文脈 |
| --- | --- | --- |
| BeginSession / EnableSelection / SelectAction / ApplyPlayerAction / ApplyEnemyAction / ShowResult | Gameplay | SessionId、Cancellation |
| OpenWindow | View。OpenからDrawへ接続するため | WindowState |
| PresentFrame | View | FrameState、WindowState |
| ChangeControl | View | FrameState |
| EndSession | Gameplay | SessionId、Finishing |
| RequestShutdown | Lifetime | 特になし |
| CloseWindow | なし | WindowState、Finishing |

全Interactionは共通の例外・取消Handler経由でLifetimeも必要とする。
従って失敗処理を含む実行依存は、ゲーム側がGameplay＋Lifetime、画面側がView＋Lifetime、
Close / RequestShutdownがLifetimeとなる。描画側とゲーム側の共通依存は全体停止へ集約される。
これは依存の役割整理であり、SystemFlowの実行所有やInteractionの明示Reaction終了をAccessへ移す案ではない。

## 実装手順

1. 現単一Contextの状態を基準に、3つの契約を定義する。旧契約の恒久的な互換階層は作らない。
2. EscapeAccessの実装宣言とWaitAsyncの名前を移行する。メソッド本体・gate・Stateの更新順は維持する。
3. Contextの3プロパティ・TryGetCore・ValidateAdditionalValueを変更し、本番呼び出しを上表に従って移行する。
4. HandlerをIApplicationLifetimeAccessへ変更する。停止と先行失敗保持の挙動を維持する。
5. テスト・計測を移行し、旧7契約と不要になったActiveSessionIdを削除する。
6. READMEと本書へ実際の改善・残る共通依存を記録する。検証後、契約再編を一まとまりとしてコミットする。

## 検証と受け入れ条件

- 通常のゲームReactionはGameplayのみ、画面側の通常FunctionはViewのみでAccess操作を完結できる。
- 例外・取消HandlerはLifetimeだけを探索し、ゲーム更新・描画契約を要求しない。
- プロパティと汎用取得が同じ値を返し、具体EscapeAccess自体の取得と固定契約・Entryによる上書きを拒否する。
- 単一Context、Noneの独立スコープ、親ありの値探索とCancellation共有、Disposeの非所有を維持する。
- 脱出・捕獲・リトライ・入力受理条件・提示待機・古い通知拒否・ポーズ中の更新抑止を維持する。
- 共有取消、Interaction間の取消、Stop失敗後のcancel / join / Close、失敗の同一性・順序を維持する。
- 既存テストを契約の都合で弱めず、共通HandlerがLifetime専用実装でも停止できることを確認する。
- 全体ビルド・全体テストを実施し、Release簡易計測で通常PresentFrameの488B/回から増えないことを確認する。
- メソッド名・取得先の変更だけなら非ネイティブ経路で確認し、描画や制御の挙動へ変更を入れた場合は実画面も確認する。

開発効率の改善量は未計測。今回期待する効果は、取得先と役割の明確化、契約横断の削減、共通停止依存の縮小。
ロック競合・GC・フローの実行固定費が、この契約再編だけで改善するとは主張しない。

## Context契約としての判断

結論: Access再分類後なら、ゲーム実行側と画面接続側のContext契約を分ける価値がある。
旧構成ではSession / Control / Presentationが共通だったが、新構成で共通になるのはLifetimeだけである。
ただし全体の実行所有者であるWindowSystemFlowは両側を扱うため、片側の契約へ無理に限定しない。
以下の契約を後続段階で実装した。元の単一Context構成とは別の変更として検証する。

### 契約は2用途、停止用の共通基底、全体用の合成

```csharp
public interface ILifetimeContext : IFlowContext
{
    IApplicationLifetimeAccess Lifetime { get; }
}

public interface IGameplayContext : ILifetimeContext
{
    long SessionId { get; }
    IGameplayAccess Gameplay { get; }
}

public interface IGameViewContext : ILifetimeContext
{
    IGameViewAccess View { get; }
}

public interface IApplicationContext : IGameplayContext, IGameViewContext
{
}
```

宣言は4つだが、用途別の公開範囲はゲームと画面の2つだけである。
ILifetimeContextはRequestShutdownやCloseの受け口にも使うため、単なる共通プロパティ抽出ではない。
IApplicationContextは両者を実行する入口であり、独自の操作を追加しない。
Lifetimeを両側へ別々に宣言せず共通基底へ置くことで、合成時の同名メンバーの曖昧さも避ける。
入力・提示・制御ごとのContextや、Interaction一つごとのContextは作らない。

### 実行ノードと引数の対応

| 対象 | 引数契約 | 理由 |
| --- | --- | --- |
| WindowSystemFlow | IApplicationContext | 画面側とゲーム側の起動・停止・合流の所有者 |
| Game / Session / TurnSystemFlow、GameFlowCheckpoint | IGameplayContext | 進行参照・待機と共通停止で完結する |
| BeginSession / EnableSelection / SelectAction / ApplyPlayerAction / ApplyEnemyAction / ShowResult / EndSession | IGameplayContext | ゲーム側Functionと共通Handlerに必要な契約を提供 |
| OpenWindow / PresentFrame / ChangeControl | IGameViewContext | 画面側Functionと共通Handlerに必要な契約を提供 |
| RequestShutdown / CloseWindow | ILifetimeContext | 全体停止のみ。CloseのWindowState / Finishingはスコープ値から取得 |
| IGameOperation.ReadAsync、IGameReaction、ISessionReaction.EndSession | IGameplayContext | SessionIdとGameplayを直接取得 |
| IGameOperation.Submit、IWindowOperation、IWindowReaction.Open / Draw、IGameControlReaction | IGameViewContext | Viewを直接取得 |
| ISessionReaction.RequestShutdown、IWindowReaction.Close | ILifetimeContext | 通常処理か共通HandlerでLifetimeを使用 |
| 共通例外・取消Handler | IFlowContextを維持 | 基盤契約に従いIApplicationLifetimeAccessだけを汎用取得 |

FrameState / WindowState / Finishingは現状の実行スコープへの追加を維持する。
それらを固定プロパティへ変えるかは別の判断とし、今回は生成手順を増やさない。
IGameOperation / ISessionReaction / IWindowReactionの各メソッドは、同じPort内でも役割に合うContext型を取れる。
引数型の違いだけを理由にPortまで追加分割しない。

### 具体Contextと構築

具体型は既存名のEscapeApplicationContextと、新設GameplayContext / GameViewContextを使う。
すべて通常のFlowContext派生で、対応するインターフェースを実装する。
内部EscapeAccessを作るのはEscapeApplicationContextだけとし、他の具体Contextは親の公開プロパティから必要なAccessを保持する。
生のEscapeAccessを公開するプロパティ、具体Contextへのキャスト、Access実体への別interfaceキャストを使わない。

| 構築箇所 | 構築するContext | 引き継ぎ |
| --- | --- | --- |
| Program | EscapeApplicationContext | 新規EscapeAccessとCancellation。SessionIdは非セッションを表す0 |
| WindowSystemFlow | GameViewContext(application, None) | View / Lifetimeのみコピーし、親探索・Cancellationは独立 |
| WindowSystemFlow | GameplayContext(application, Cancellation) | Gameplay / Lifetime / 対象IDをコピー。取消だけ共有し、広い親探索は遮断 |
| GameSystemFlow | GameplayContext(game, newSessionId, All) | Gameplay / Lifetimeと新ID。値探索・Cancellationはゲーム側の親からAllで引き継ぐ |
| ゲーム側終了処理 | GameplayContext(session, None) | 同じ対象・Access、独立したCancellationと追加値 |
| 停止・Close用スコープ | GameViewContextの独立 / 親あり構築 | 停止はNone、Closeは同種の親をAllで探索しWindowStateを共有 |

上表のAccessのコピーは参照の保持であり、Stateの複製・別ロック・別管理オブジェクトの生成ではない。
親の実装型が何であっても、宣言したContext契約だけで構築できることを条件にする。

セッション生成は、具体ContextのCreateSessionContextを呼ぶ代わりに次の形へ変更する。

```csharp
var sessionId = context.Gameplay.CreateSession();
using var session = new GameplayContext(context, sessionId, ContextInheritance.All);
```

CreateSessionの停止との競合時の例外、初期化・公開の分離、リトライ前のEndSessionは維持する。
コンストラクターは生成と参照の構成に限定し、Reactionの実行や提示待機を行わない。
CreateIndependentContext等も具体型のコンストラクターへ置き換え、Contextインターフェースへ生成メソッドを要求しない。

### プロパティと探索の公開範囲を一致させる

インターフェースの引数へ変えただけでは、実体が広いままならTryGetで別Accessを取得できる。
そのため、WindowSystemFlowから先は上表の限定された具体Contextを渡す。
GameplayContextはGameplay / Lifetime、GameViewContextはView / LifetimeだけをTryGetCoreで返す。
実フローでは両者とも広いApplicationContextの値は探索せず、Cancellation共有はCoreのモードで別途維持する。
さらにTryGetCoreで反対側の固定Accessを明示的に拒否するため、Allを指定した構築でも隠した契約へ探索が抜けない。
同種の子・終了スコープのみ親探索を許可し、固定3AccessとEntryの追加による迂回は検証で拒否する。

これにより、通常のプロパティ参照とContext探索の双方で役割を分けられる。
ただし返した共有Access実体への明示的キャストや、利用者が別のIFlowContext実装を渡す場合まで隔離する保証ではない。
CancellationとIFlowContextの探索機能自体は共通の基盤契約として残る。

### 段階と評価条件

1. まず前段の3Access再分類を単一Context上で検証する。ここではContext契約の効果と混ぜない。
2. 続けて上記Context契約と具体型・コンストラクターを導入し、ノードとPortの引数を切り替える。
3. 具体型・生成メソッドへの依存が各実行本体から消え、コンストラクター呼び出しだけに限定されることを確認する。
4. Gameplay側からView、View側からGameplayのTryGetが失敗すること、固定契約の追加拒否を確認する。
5. root→ゲームのCancellation共有、フレーム・終了の独立Cancellation、親の値探索遮断、Disposeの非所有を確認する。
6. 既存の終了・取消競合テストと全体テスト、フレーム計測を維持し、前段と別のコミットで評価できるようにする。

評価上の利点は、ゲーム側には画面通知・入力配送が見えず、画面側には論理更新・待機が見えないこと。
残る共通文脈は意図した全体寿命と基盤のCancellationであり、以前の大きな共通契約より境界を説明しやすい。
代償は限定された具体Context2種類と合成ルート、生成時の参照保持・検証コードが増えること。
ここが役割の明確化に見合うかを前段との比較で判断し、さらに細かなContext分割は行わない。

関連: [現在の実装と操作](../../InteractionFlow.Samples.EscapeGame/README.md)、
[これまでの利用側改善](./ESCAPE_GAME_APPLICATION_REFACTOR_PLAN.md)、
[利用設計指針](../guides/INTERACTIONFLOW_APPLICATION_DESIGN.md)。
