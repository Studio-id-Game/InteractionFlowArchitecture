# EscapeGameのResultチェーン再設計プラン

> **完了済みの設計記録**
> Result API の移行は 2026年11月17日に完了しました。本文は検討当時の記録です。
> 現行の契約は [Result API](../../docs/RESULT.md)、経緯は [移行記録](../../docs/RESULT_API_MIGRATION.md) を参照してください。

作成日: 2026-11-11。
基準コミット: `02a70f8`（局所検証のResult化、TryAllocateSession、回帰テストと調査記録）。
この文書は設計・レビュー・実装結果を記録する。Port契約とCore APIは維持する。

## 実装前セルフレビューと確定した契約

プランをレビューし、次の未確定点を解消してから実装する。

| 指摘 | 修正した設計 |
| --- | --- |
| 失敗Resultは起動済みTaskを保持しない | Windowは起動時に所有用ローカルへTaskを登録し、finallyで所有者へ渡す。正常時はResult<Task<InteractionEnd>>の成功値としてフレーム反復へ渡す。通常データの退避ではなく、失敗時にも必要な所有情報として区別する。 |
| 送出例外のResult化を早めると集約のラッパー構造が変わる | Windowの実行所有境界では生の送出例外をFailureCollectorへ記録して終了経路へ回復する。返却された失敗ResultだけをThenErrorで記録する。同じ失敗を二重に記録しない。 |
| cancelが送出するとjoinを飛ばし得る | cancelの例外は記録し、joinへ進む。stopの返却失敗はThenErrorで保持してからcancel/joinへ進む。joinの返却失敗も別のThenErrorで保持する。 |
| 終了処理自身の失敗で終了処理を再実行し得る | Closeは集約結果を受け取って一度だけ実行し、EndSessionは最終のResolveAsyncで一度だけ実行する。 |
| Interactionで失敗時の終了トークンが失われる | 現行の直接返却と同一性を維持する。新しい継続型や可変の進行フラグが必要になるため、両Interactionは今回変更しない。 |

確定した段階のシグネチャは以下とする。

- `RunWindow(..., FailureCollector, out Task<InteractionEnd>?) -> Result`:
  Open成功→起動Task→フレーム反復を内部でThen接続。返却失敗は呼び出し元へ、送出例外は記録して回復。
- `RunFrameLoop(..., Task<InteractionEnd>) -> Result`:
  フレーム/制御の返却失敗、または正常停止を返す。Task所有と終了処理は担当しない。
- `StopAndJoinGameplay(..., Task<InteractionEnd>?, FailureCollector) -> Result`:
  stop→ThenErrorで記録→cancel/join→ThenErrorで記録。進行用の成功を返す。
- `CloseWindow(..., Result previous, FailureCollector) -> Task<InteractionEnd>`:
  先行結果をReactionへ渡し、閉鎖Interactionをawaitする。呼び出し側も完了をawaitして親Contextの寿命を保つ。
  送出時は従来のCollectorの順序と集約構造を維持して再送出。
- `RunSessionAsync(IGameplayContext) -> Task<Result<bool>>`:
  成功したBeginのInteractionEndを次段階へ渡し、セッション結果をリトライ意図へ変換する。
  自身への要求済みキャンセルはfalseの成功、その他の送出例外は失敗Result。
- `EndSessionAsync(IGameplayContext, Result previous) -> Task<InteractionEnd>`:
  成功・失敗の両経路で一度だけ実行し、先行失敗と終了時の送出例外を維持する。

Windowは`RunWindow(...).ThenError(...).Then(...).Then(failures.ToResult)`を変数に受け取り、`CloseWindow`へ直接渡す。
Gameplayは反復の判断に使う終了結果とリトライ意図だけをローカルへ受け取り、
実行ヘルパーはreturnからResult<bool>まで接続する。最後のResolveAsyncの両経路は、渡す先行結果と意図が異なる。
FailureCollectorでは、記録後に何を返すかをメソッドで選ぶ。

- `Add(Exception)`は渡された例外を記録し、同じ`Exception`を返す。呼び出し側で`Result`または`Result<T>`の失敗へ暗黙変換できる。
- `Add(Result)`は失敗を記録し、受け取ったResultをそのまま返す。成功時の記録件数は増えない。
- `RecordAndContinue(Exception)`は失敗を記録し、終了手順を進めるための`Result.Success`を返す。

`.ThenError(error => failures.Add(error))`は失敗の記録と伝播に使う。
`.ThenError(failures.RecordAndContinue)`は失敗を保持して後始末へ進み、最後に`ToResult()`で集約結果を返す。

汎用Captureや新しい実行状態クラスは追加しない。

## 実装後セルフレビュー

実装は上記の確定契約に基づいて行った。次の点を差分・呼び出し経路・回帰テストで再レビューした。

| 観点 | 確認結果と対応 |
| --- | --- |
| returnからの接続 | Windowの実行、停止・合流とGameplay実行はreturnから接続。Window本体は集約結果をCloseWindowへ直接渡し、Gameplayの終了結果タプルは次の反復の判断に使う。 |
| 成功値の受け渡し | Windowは起動Taskを次のフレーム反復へ渡す。Gameplayは成功したBeginのInteractionEndを継続判断に使い、セッション結果をリトライ意図へ変換する。 |
| 後始末への回復 | 返却失敗はThenErrorで記録し、進行用成功へ回復。Close前のThen(FailureCollector.ToResult)で失敗を戻す。例外捕捉で記録済みの失敗は再記録しない。 |
| 二重実行 | Stopの後のThenErrorはStopを再実行しない。Close/EndSessionの後に同じ終了処理への回復を置かない。 |
| 失敗時のTask所有 | RunWindowのfinallyで、フレームの送出例外を含む全経路について起動済みTaskを返す。cancel失敗でもjoinを試みる。 |
| 同一性と集約 | Windowの生の送出例外は直接Collectorへ記録。返却失敗のResultExceptionは同一参照を維持する。Closeの送出時も従来のCollectorの順序と階層を保つ。 |
| 正常停止と取消 | 正常停止を失敗にせず、Gameplayの継続確認が停止を返す場合はリトライしない。要求済みキャンセルと無関係なキャンセルの区別は既存経路を維持する。 |
| Interactionの適用可否 | 両Interactionのソースを維持。失敗時の終了トークン保管や継続ラッパーを追加せず、直接返却の契約を優先した。 |

レビューで不足と判断したテストを3ケース追加した。
返却されたOpen失敗と、Stopの返却失敗/送出例外、Closeの追加例外について、
同一参照・順序・既存の集約階層・終了処理の一回実行を確認する2ケースと、
キャンセル通知のコールバックが失敗してもゲームを合流してCloseへ進むケースである。
既存ケースと合わせて対象テストを実行した後、再度差分を確認した。

記述量はSystemFlowの目的別の関数分割により増えるが、実行と終了の接続順を本体に集約し、
失敗記録から終了手順への回復をThenErrorで明示した。
Taskと失敗の所有以外の可変の進行状態、新しいContext値、Port/Core APIの追加はない。
現在のレビュー範囲で未対応の不具合・契約違反は見つかっていない。
これは任意のスレッド実行順序や外部ネイティブ障害を網羅する証明ではない。

## 検証結果

- `dotnet build InteractionFlow.slnx`: 警告0・エラー0。
- EscapeGameの個別テスト: 155件成功。非同期の閉鎖例外でも親Contextを保持し、先行失敗と集約するケースを含む。
- `dotnet test InteractionFlow.slnx --no-build --no-restore`: 全625件成功、失敗・スキップ0（EscapeGame155、他470）。
- `git diff --check`: 問題なし。
- 両InteractionとCore/Standardにソース差分がないことを確認。

既存のFlowTestsで12ターンの解法・リトライ・ポーズと再開・停止と終了を実行した。
描画はテスト用Reactionで代替し、呼び出しスレッドも検証している。
実GUI操作とゲーム全体の性能再計測は実施していない。

## 目的と今回の基準

成功時に次の段階が必要とする値を返し、その受け渡しをResultで構成する。
一つの戻り値へ接続できる範囲は、`return`から始まるチェーンで完結させる。
元の手続きの変数・分岐を残したままResult APIを加える方法は採らない。

利用意図の一次資料は、今回ユーザーが提示した旧Notepadのコード例とする。
読み込み→選択→Entry取得→編集・保存→最終応答がResultで接続され、
二つの値が必要な段階ではタプルを渡し、局所的なエラー変換はその処理の近くにある。
Door系や現在の他サンプルを、意図されたResult用法の正解とはみなさない。
旧例のキャンセルをExceptionで返す箇所まで現在のゲームの仕様へ転用するものではない。

今回取り消した具体案と、その検証結果は
[利用調査](./ESCAPE_GAME_RESULT_API_REVIEW.md)に残す。
そこにある撤回判断は、その実装案の不採用であって、Resultによる表現が不可能という判断ではない。

## 書き方の方針

- 最終結果を返すためだけの`var result = await ...; return result.Resolve(...);`を作らない。
  接続できる場合は`return await ...ThenAsync(...).ResolveAsync(...);`とする。
- 成功値は次段階の入力として使う。複数の値が必要ならタプルで渡す。
  値を渡せる箇所を、外側の可変変数やContextへの一時保存で代替しない。
- 同期の値変換はMap、Resultを返す次段階はThen、失敗の変換・回復はThenError、
  最終的な応答・終了への取り出しはResolveを使う。非同期も同じ区別を保つ。
- Resultを返す関数をMapへ渡して入れ子にしない。OnSuccessへ非同期ラムダを渡さない。
- 補助関数は「開始」「ターン反復」「停止」「合流」「閉鎖」の目的で切り、
  各関数は次段階へ必要なResultを返す。Thenを包むだけの接続用ヘルパーは追加しない。
- 反復はwhileでもよい。一回のセッションの処理や、一つの実行の終了処理をチェーンにする。
  ループを再帰や無理なLINQ表現へ置き換えない。
- Taskや資源の所有、失敗後も必要な情報など、寿命上の理由があるローカル値は許容する。
  変数をなくすために、同じ情報を持つ巨大な実行状態クラスを新設しない。

現在のCoreには、値なしResultから成功値を作るMapがない。
必要なら既存の`result.Then(() => value.AsResult())`を使う。
汎用Mapの追加は、実際の段階契約を示した後の独立したAPI提案とし、このプランでは前提にしない。

## 最初に決める段階契約

コードを書く前に、以下の表を具体的な型とシグネチャで埋める。
型名は仮称であり、それぞれを新しいクラスにするという意味ではない。
既存の型・タプル・boolで責務を表せるなら、それを優先する。

| 対象と段階 | 必要な入力 | 次へ渡す成功値 | 失敗・正常停止の扱い |
| --- | --- | --- | --- |
| Window: Open | View用Context、WindowState | 開いたウィンドウの実行に必要な情報 | 起動失敗でも停止・閉鎖を試みる。未起動を識別できること。 |
| Window: ゲーム起動 | Gameplay用Context | 起動したTaskと、その所有者への登録 | 起動後に失敗してもTaskを合流できること。 |
| Window: フレーム反復 | 起動済み実行、View/制御Context | 後始末へ移れる実行情報、または値なし成功 | ユーザー終了・ゲーム完了は成功。フレーム/制御の失敗は失敗Result。 |
| Window: 停止要求 | 実行所有者、蓄積した先行失敗 | 待機の解除後に必要な実行情報 | 停止失敗でもキャンセル・合流・閉鎖を試みる。 |
| Window: cancel / join | 所有するTaskとCancellation | 後始末の集約結果 | cancel失敗でもjoinを実行し、両方を保持する。未起動ならjoinしない。 |
| Window: Close | 先行結果、View用Context | 実際のCloseが返すInteractionEnd | 先行失敗と閉鎖失敗を保持する。再実行しない。 |
| Gameplay: 確保・開始 | Gameplay用ContextとセッションID | 実行可能なセッション情報 | 確保時の停止は正常終了。Beginの失敗はEndSessionへ渡す。 |
| Gameplay: セッション反復 | 開始したセッション | リトライ意図など、終了後の経路に必要な値 | 勝敗・正常停止・失敗を混同しない。 |
| Gameplay: EndSession | セッション、先行結果、リトライ意図 | 終了InteractionEndと次の反復の判断材料 | 成功・失敗の両方で実行し、終了失敗時はリトライしない。 |

各段階で「次の段階が使わない成功値」を惰性で持ち回らない。
逆に、失敗Resultへ変わることで失われる値を、後始末が必要としないか必ず確認する。

## 失敗を保持して後始末へ接続する

ThenErrorは先行失敗の記録と、後始末へ進むための回復に使える。
失敗しても終了処理を続けるという理由だけで、ThenErrorの利用を除外しない。
外側へ記録を出す場合は、FailureCollectorのように、その実行の最終結果を所有するものへ限定する。
段階の通常の値の受け渡しまで、この記録領域へ移さない。

以下は接続順をレビューするための擬似コードであり、現存メソッドや確定シグネチャではない。

```csharp
var outcome = RunOwnedWindow(lifetime)
    .ThenError(lifetime.RecordFailureAndContinue)
    .Then(() => RequestShutdown(lifetime))
    .ThenError(lifetime.RecordFailureAndContinue)
    .Then(() => CancelGameplay(lifetime))
    .ThenError(lifetime.RecordFailureAndContinue)
    .Then(() => JoinGameplay(lifetime))
    .ThenError(lifetime.RecordFailureAndContinue)
    .Then(lifetime.Failures.ToResult);
return CloseWindow(lifetime, outcome);
```

この例のRunOwnedWindowは、内部でOpen→ゲーム起動→フレーム反復を値付きResultで接続する。
後始末へ入る時点では、その所有者がTaskと資源を保持していることが前提になる。
lifetimeはその所有責任を示す仮称であり、新クラスの導入を決定したものではない。

RecordFailureAndContinueは、失敗を記録した後に進行用のResult.Successを返す。
終盤のFailures.ToResultで、記録していた失敗を最終結果へ戻す。
これはアプリケーションの成功への回復ではなく、終了手順を継続するための局所的な回復である。
Closeは最後に一度だけ呼び、その返却した終了結果を使う。

`Then(Cleanup).ThenError(Cleanup)`のような接続は避ける。
Cleanup自身が返した失敗でThenErrorが発火し、Cleanupを二重実行し得るためである。
上のように各終了処理の後で失敗を保持し、次の別の処理へ進む順序を明示する。

Gameplayでは、リトライ意図を成功値として終了段階へ渡す。
成功時と失敗時でEndSessionへ渡す引数が異なるなら、最後のResolveAsyncで二つの経路を記述してよい。
ThenErrorを使う場合は、失敗を保持したうえで「リトライしない」という成功値へ回復する。
どちらも、途中でReactionEnd/InteractionEndへ戻してから再びResultへ包む構成にはしない。
具体案を比較してから、より小さく読める方を選ぶ。

## 送出例外、キャンセル、資源の所有

[Resultの契約](../../docs/RESULT.md)を維持する。
ThenErrorは失敗Resultに反応し、コールバックの送出例外やキャンセル済みTaskを捕捉しない。
連結関数へ自動捕捉を追加しない。

現在のSystemFlowは、実行所有者として伝播した例外も集約し、終了を試みる境界である。
この責務を残す場合、try/catchは実行を所有する範囲に置く。
単にcatchを短く見せるため、Resultを返す各委譲先を汎用のCapture関数で包まない。
例外によりチェーンを離れた場合にもTaskの停止・合流とClose/EndSessionが必要なので、
その経路をreturnチェーンと合わせて先に図示・表記する。
catchを補助関数へ移しただけの差分は、今回の改善の達成とは扱わない。

- 返却された失敗、送出例外、Taskキャンセルを区別する。
- 自身への要求済みキャンセルの通常停止と、無関係なキャンセル失敗を区別する。
- キャンセル処理の成功をリトライ・継続の根拠にしない。
- OpenからCloseまでのraylib呼び出しは同じスレッドに保つ。
  Windowの経路をThenAsync/ResolveAsyncへ移さない。
- Resultが失敗しても起動済みTaskを失わない。起動時点で実行所有者に登録する。
- ContextのDisposeを停止・合流の代用にしない。先行失敗と後始末の失敗を両方保持する。
- 既存ResultExceptionの同一性、原因例外、観測順序を保つ。
  集約階層を変更する必要があるなら、コード変更前に契約差分として明記する。

## Interactionは段階契約から再評価する

PlayTurnとHandleResultは現在の実装を維持したまま、次を設計する。
SystemFlowの改善が成立したという理由で、自動的にチェーン化しない。

| 対象 | 検討する値の流れ |
| --- | --- |
| PlayTurn | 選択開始→入力・確定→プレイヤー行動→継続可否と勝敗→必要なら敵行動→最終ReactionEnd |
| HandleResult | 結果提示→受付可能性→受付開始→リトライ入力→最終ReactionEnd |

正常停止は失敗に変えず、成功側の分岐として扱う。
停止時に残りのチェーンを実行しない方法を、成功値の形と段階の責務から決める。
各Thenに外側のcanContinueを読み書きさせる構成や、段階ごとの同じ継続確認の追加を解決策にしない。
局所的に終端を選ぶための分岐・ネストは許容するが、Resultと終了トークンの往復を繰り返さない。

現行契約では、失敗Resultは成功値を持たず、失敗を返したReactionEndをそこから取り出せない。
また、Interactionが返す終了トークンはReactionが生成し、既存の直接返却と同一性の規則がある。
この点は未解決の設計項目として扱う。

1. どの段階がReactionEndを必要とし、どの段階は通常の値・Resultだけで進められるか整理する。
2. 失敗した元のReactionEndを返す経路と、その保管責任を明示する。
3. 保管用変数を一つ残す必要があるなら、その理由と増減する記述を示す。
   契約上必要な保管まで「無駄な変数」として消さない。
4. 最後に別Reactionから終了を取得する案は、既存の直接返却・同一性との契約差分になるため、
   単なるリファクタリングとして採用しない。
5. 契約維持と可読性を両立できなければ、該当Interactionは変更しない。

MapでResult<ReactionEnd>へ持ち上げる案も、成功値が次段階に必要な箇所で使う。
単に失敗判定を繰り返すための包み直しには使わない。
Portの返却型変更や専用の継続型を必要とする場合は、その必要性を別案として示す。
巨大な進行ラッパーや新しいCore APIで問題を隠さない。

## 作業の順序

1. 基準コミットに対して、段階の入力・成功値・失敗・正常停止・所有の表を完成する。
   代表経路をreturnチェーンの擬似コードで示し、使用する既存Resultオーバーロードと対応付ける。
2. WindowSystemFlowの一つの実行について、通常経路とThenErrorからの終了経路を設計する。
   起動途中の例外、未起動Task、cancel失敗後のjoin、Close自身の失敗までを追う。
3. GamePlaySystemFlowの一回のセッションを設計する。
   リトライ意図を値で渡し、EndSessionの失敗で次のセッションを作らないことを示す。
4. 差分案の全体で、変数退避・重複した終了処理・同じResult変換・補助関数を点検する。
   既存より記述が増えるなら、何の責務を明確にしたかを具体的に説明する。
5. SystemFlowの設計を確認後、Interactionの契約表と失敗時の終了トークンを検討する。
6. 実装を行う段階になったら、一対象ずつ変更・テストし、他の案へ自動的に広げない。
   このプランの作成時点では実装しない。

## 完了条件と検証

変更前の基準は、全体ビルド警告0・エラー0、全621テスト成功（EscapeGame151、他470）。
既存のFlowTests / CleanupTests / FailureTests / ResultInteractionTestsを利用する。

- 最終値を返すだけの中間変数がなく、通常の段階間の値がチェーン内で渡る。
- 主処理失敗後も停止→cancel→join→closeを試み、終了処理を二重実行しない。
- Open/Beginの返却失敗、フローまでの送出例外、各終了処理の追加失敗を保持する。
- セッション再試行、勝敗確定、ポーズ・再開、段階境界の停止・取消を維持する。
- 先行失敗のResultExceptionと終了トークンの同一性を、現在の契約どおりに保つ。
- 原因を失った成功、無関係なキャンセルの回復、同じ失敗の二重集約がない。
- Windowの呼び出しスレッドと、Task・Context・資源の所有を維持する。

将来の実装後はAGENTS.mdに従って全体ビルド・全体テストを実行する。
新しいテストは観測可能な契約の不足だけを補い、チェーンの呼び出し回数や補助関数の配置は固定しない。
描画スレッド・資源寿命に変更が及ぶ場合は実GUIでも確認する。
文書は採用した最終設計を説明し、試行案を現行実装として書かない。

関連: [利用設計](../../docs/guides/INTERACTIONFLOW_APPLICATION_DESIGN.md)、
[描画環境への接続](../../docs/guides/RENDERING_LIBRARY_INTEGRATION.md)、
[Result API](../../docs/RESULT.md)、[終了結果](../../docs/REACTION_RESULTS.md)。
