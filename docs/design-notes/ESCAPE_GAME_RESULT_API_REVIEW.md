# EscapeGameのResult API利用調査と改善案

確認日: 2026-11-11。対象は現在の作業ツリーのEscapeGameと既存テスト。
当初の調査・提案と、その後の実装結果を記録する。

最新の採用内容は[Resultチェーン再設計プランとレビュー結果](./ESCAPE_GAME_RESULT_CHAIN_PLAN.md)を参照する。
基準コミット02a70f8から、値の受け渡しとThenErrorによる後始末を再設計し、両SystemFlowへ適用した。
以下はその前に行った試行と撤回の記録である。両Interactionは引き続き変更していない。

## 実装結果

2026-11-11、以下の変更を適用した。後続の「現状」「改善案」は変更前の調査と設計判断の記録であり、
案1・4は実装済み。案2・3のフローのチェーン化は試行後に撤回した。案5は既存の例外処理方針の確認を記録した。

| 案 | 適用した内容 |
| --- | --- |
| 1: 局所検証 | ValidateUpdate / ValidatePresentationをResult返却にし、提示キー検証をThenで接続した。 |
| 2: SystemFlow | 部分的な連結と実行境界・終了処理のチェーン化を試行したが不採用。変更前の分岐とtry/catch/finallyへ戻した。GamePlaySystemFlowのTryAllocateSessionへの対応だけは維持する。 |
| 3: Interaction | ContinueAfterAsyncとResult<ReactionEnd>のチェーン化を試行したが不採用。PlayTurn / HandleResultは変更前の継続判定と早期returnへ戻した。 |
| 4: セッション確保 | AllocateSessionをTryAllocateSession(out long sessionId)へ置き換えた。停止後はfalseとID 0を返し、GamePlaySystemFlowの例外による正常停止判定を除いた。テストと計測用の呼び出し側も更新した。 |
| 5: 例外境界 | フロー所有者が送出例外を最終Resultへ集約する既存の方針を確認し、この文書へ記録した。例外境界の再構成は撤回した。 |

案5の境界は、通常のResult連結とは別の実行所有責任である。
Interaction内の送出例外は既存のException/Cancellation Portが扱い、そこを越えてフローへ伝播した例外は、
実行所有者が停止・合流・終了を試みたうえで最終結果に含める。
Then/Resolveに例外捕捉を追加せず、raylibのExternalに包括catchを追加する変更も行っていない。
GameSessionSystemFlow、PresentFrame、単一Reactionへ委譲するInteraction、Core/StandardのAPIは維持した。

失敗経路の回帰テストを6ケース追加した。
Open/Beginの失敗Resultが後続を止め、元のResultExceptionを終了処理まで保持することを確認する。
結果提示/入力受付では、通常例外とOperationCanceledExceptionを格納した失敗Resultの両方について、
元のReactionEndを返し、入力を読まず、送出例外・Taskキャンセルの処理へ入らないことを確認する。
既存の停止後確保テストと検証補助のテストは新契約に更新した。

## SystemFlowの最終判断

SystemFlowのResultチェーン化も撤回した。今回の具体案では、正常停止・再開待機・後始末・
実行の所有・描画スレッドの制約を扱う変換と補助処理が増え、利点が十分に得られなかったためである。
WindowSystemFlowはHEADの実装へ戻し、GamePlaySystemFlowもセッション確保のTryAllocateSession対応以外は戻した。
GameplayReactionの局所検証のResult化、TryAllocateSession、意味のある回帰テストは維持する。
今回の検討はResultチェーンを増やすこと自体を目標にせず、局所的な失敗伝播が簡潔になる箇所で使う判断とした。

「失敗しても後始末を続ける」こと自体がチェーン化を妨げるわけではない。
ThenErrorで先行失敗をFailureCollectorへ保持し、進行用の成功Resultを返してからThenで後始末へ進み、
最後に記録した失敗を含めて結果を確定する構成も可能である。
この案を十分検討せず、終了処理はResolveでなければならないと判断した点は訂正する。
送出例外の捕捉はThenErrorの機能ではなく、別の明示的な境界として必要になる。
現在の復元判断は、試行した具体案の不採用であり、Result APIの表現能力の限界を結論するものではない。

## SystemFlowの実行境界と終了処理の再整理（不採用の試行記録）

GamePlaySystemFlowは実行本体のtry/catchをRunSessionAsyncへ集約した。
Begin→SessionをThenAsyncで連結し、成功時のリトライ意図をResult<bool>として返す。
自分自身への要求済みキャンセルは成功値false、それ以外の送出例外は失敗Resultにする。
本体はその成功・失敗をResolveAsyncでEndSessionAsyncへ接続する。
終了結果とリトライ意図をタプルで受け取り、終了失敗・終了要求がなければ次のセッションへ進む。
EndSessionAsyncの追加失敗は先行失敗を保持して再送出し、終了トークンは終了Reactionのものを使う。

WindowSystemFlowはRunWindowでOpen→ゲーム起動→フレーム反復をThenで接続し、
送出例外もこの実行境界でResultへ確定する。
起動済みTaskは、フレームが例外終了した場合もfinallyで呼び出し元へ渡す。
本体は二回の同期Resolveで、成功・失敗にかかわらずStopAndJoinGameplay→CloseWindowへ接続する。
StopAndJoinGameplayは先行Resultに停止・キャンセル・合流の失敗を集約して返す。
ここをThenへ変更すると先行失敗時に後始末が省略されるため、両経路を解決するResolveを使う。
OpenからCloseまで非同期チェーンを使わず、呼び出し元スレッドを維持する。

Result API自体は例外を捕捉しない。実行所有者の捕捉、後始末の継続と失敗集約、
最終終了処理の失敗の再送出は、各privateメソッドの明示的な境界に残す。
失敗Resultの回復やThrowIfErrorによる例外化は追加しない。
Windowの送出例外は停止処理より前にResult化されるため、複合失敗のラッパー階層は従来と異なる場合がある。
先行ResultExceptionと原因例外の参照を保持し、集約をflattenせず、実行→停止→合流→閉鎖の順序で保持する。

例外Portが再送出する構成で、Open・フレーム・Beginの各例外とCloseの追加例外の組み合わせを6ケース検証した。
さらにBeginの返却失敗とEndSessionの送出例外が、ウィンドウ閉鎖まで保持されるケースを追加した。

## Interactionの最終判断

PlayTurn / HandleResultのResultチェーン化は撤回した。
終了トークンの保持、成功Resultである正常停止の別判定、Resultとの相互変換が必要になり、
元の継続判定と早期returnに比べて記述量と状態管理の負担が増えたためである。
両ソースは今回の変更前へ戻し、GameplayCheckpoint.WaitToContinueAsyncによる明示的な判定を維持する。
ContinueAfterAsyncは追加しない。失敗伝播・終了結果保持・正常停止を確認する回帰テストは維持する。
GameplayReactionの局所検証、TryAllocateSession、既存の例外境界の説明は維持する。SystemFlowの連結も最終的に撤回した。

## Interactionの再検討と再修正（不採用の試行記録）

初回のContinueAfterAsyncは、Result.ResolveAsyncで条件分岐を包む構造だった。
次段階がResultを返さず、Interaction本体からResultの接続も読み取れないため、これを削除した。
HandleResultはさらに、途中でReactionEndへ戻す二重のチェーンを一本化した。
以下は撤回前の構成であり、現在のHandleResultには適用していない。
提示結果をResult<ReactionEnd>へ変換し、受付開始とリトライ待機を二つのThenAsyncで接続する。
最後のReactionEndの保持とResolveは一箇所にまとめ、EnableInputAndWaitForRetryAsyncとWaitForRetryAsyncは削除した。

`end.Result.Then(() => end.AsResult())`は、成功時だけ終了トークンを値へ変換する。
概念上は値なしResultのMapに相当するが、そのオーバーロードは現在のCoreにないため既存のThenを使う。
戻り値はResult<ReactionEnd>となり、失敗は外側のResultで伝播してThenAsyncの後続を抑止する。
ReactionEndを無条件にAsResultで包む方法は使わない。

失敗Resultは成功値を持たないので、失敗を返した最後のReactionEndをローカル変数endへ保持する。
最後のResolveは成功時の終了トークン、または保持していた失敗時の終了トークンを返す。
ThenAsyncの途中で失敗を成功へ回復せず、終了時の取り出しだけをResolveで行う。
返却失敗、送出例外、Taskキャンセルの区別と元の終了トークンの同一性を保つ。

正常停止は成功なので、ThenAsync内のチェックポイントで判定してその終了トークンを返す。
PlayTurnは選択開始→選択・行動と、プレイヤー行動→敵行動の二つの接続を持つ。
HandleResultは提示後の継続判断をcanContinueに保持し、正常停止では受付を開かず、
次のThenAsyncでもチェックポイントや入力待機を実行せず終了する。
通常の失敗はResultが後続コールバックを抑止する。正常停止のために失敗Resultを作らない。
入力反復はwhileを保ち、再開待機やチェックポイントの回数・位置を増やさない。

この構成は終了トークン保持とResult<ReactionEnd>への変換が必要なので、元の早期returnより短くはない。
Resultチェーンの失敗伝播を直接利用する例として採用し、新しいCore APIや汎用の段階接続APIは追加しない。
GameplayCheckpointは停止・再開判断だけを担当する。

## 調査時の結論

成功・失敗の伝播と終了結果の集約にはResultが使われている。
一方、`Then` / `Map` / `Resolve`などによる局所処理の連結は使われていない。
「Resultを利用していない」とは評価しないが、Result APIの連結を示すサンプルとしては改善の余地がある。
GameplayReaction内部の検証結果のResult化に加えて、SystemFlowでも成功時だけ進める区間に
Resultチェーンを採用する。InteractionではPlayTurnとHandleResultの段階接続にも適用する。
反復・停止・合流の構造を残し、その内側の段階を連結する。

判断基準は[Result API](../RESULT.md)と[Reactionとフローの結果](../REACTION_RESULTS.md)。
`ReactionEnd` / `InteractionEnd`はResultを保持するが、Resultから自由に生成できる同義の型ではない。
単純な中断判定には`HasException`を使うという既存方針にも従って評価した。

## 現状

| 対象 | 利用状況と評価 |
| --- | --- |
| WindowSystemFlow | Open・Frame・Control・ゲーム実行の`end.Result`をFailureCollectorへ集め、Closeへ先行Resultを渡す。失敗後もstop/cancel/join/closeを試みる構造は適切。 |
| GamePlaySystemFlow | Begin/Sessionの結果を`outcome`へ保持し、EndSessionへ渡す。失敗時はリトライしない。送出例外をResultへ変換する境界もここにある。 |
| GameSessionSystemFlow | PlayTurnの終了結果とGameplayCheckpointで中断を決め、最後にHandleResultへ進む。Resultの直接操作はないが、失敗結果を保持した終了トークンを返している。 |
| PlayTurn / HandleResult | 各Reaction後のチェックで失敗時に同じReactionEndを返す。失敗をThrowIfErrorで投げ直していない。 |
| BeginSession / HandleControlInput / OpenWindow / RequestShutdown | 一つのReactionへ委譲する構成。局所的なResult連結を加える必要はない。 |
| EndSession / CloseWindow | CleanupInfoの種別を検証し、先行Resultを終了Reactionへ渡す。種別不一致は既存の例外処理Portへ委譲される。 |
| PresentFrame | 入力取得・配送後に描画Reactionを返す。配送のboolは入力の受理可否であり、処理失敗Resultとは異なる。 |
| GameplayReaction | 更新前提の違反をReactionの失敗として返すが、内部のValidateUpdate/ValidatePresentationは`Exception?`を返し、nullで成功を表す。 |
| FailureCollector / 終了Reaction / 例外・キャンセル処理 | `Try`、`Result.Success`、例外からの変換を利用。先行失敗と後始末の失敗を集約し、単一の既存ResultExceptionも保持する。 |

SystemFlowsは3型、Interaction派生は9型を確認した。GameplayCheckpointは補助クラスとして別に確認した。
EscapeGame本体では`Then` / `ThenAsync` / `ThenError` / `ThenErrorAsync` / `Map` / `MapAsync` /
`Resolve` / `ResolveAsync` / `OnSuccess` / `StartAsync` / `AsResult` / `ThrowIfError`の呼び出しは見つからなかった。
`GetAwaiter().GetResult()`はTaskの同期完了待機で、Result APIの利用件数には含めない。

## 改善案1: 局所的な検証をResultで連結する（優先）

対象: [GameplayReaction](../../InteractionFlow.Samples.EscapeGame/Aspects/Gameplay/Externals/GameplayReaction.cs)
のValidateUpdate / ValidatePresentationとその呼び出し側。

現在の`Exception?`でも正しく失敗を返せているため、不具合修正ではなく表現の統一として実施する。

1. ValidateUpdateとValidatePresentationを`Result`返却へ変更する。
2. 成功は`Result.Success`、前提違反は例外からの暗黙変換で返す。
3. ValidatePresentationはValidateUpdateの成功時だけ提示キーを検証する`Then`で構成する。
4. Reaction側は検証失敗を`GetEnd(validation)`へ直接渡す。Stoppedの正常終了分岐は残す。

想定する連結の形（提案であり未適用）:

```csharp
internal static Result ValidatePresentation(GameUpdateResult change, string operation)
    => ValidateUpdate(change.Status, operation).Then(() =>
    {
        if (change.Status == GameUpdateStatus.Applied && change.Presentation is null)
            return new InvalidOperationException($"{operation}: an applied update requires a presentation key.");

        return Result.Success;
    });
```

Applied / Stoppedは成功、許可されたRejectedは通常応答、予期しないRejected・Deferred・未知の値は失敗、
Appliedで提示キーがなければ失敗、という既存の意味を維持する。
Deferredは更新ループで再試行する値であり、その制御をThenへ押し込めない。
検証後の非同期提示待機も、停止・ポーズ・キャンセルの分岐が読める現在の構造を保つ。

検証: GameplayReactionTestsのnull/例外型の直接assertを`Try`と原因例外の確認へ更新する。
既存の前提違反・未知の更新状態・提示キー不足・Stoppedのケースを維持し、
FlowTestsの失敗後に次段階を実行しないケースとResultInteractionTestsも実行する。

## 改善案2: SystemFlow内の成功時の進行をResultチェーンで表す（優先）

### GamePlaySystemFlow: BeginからSessionへの進行

この区間は最終的にInteractionEnd自体ではなく、`outcome`とリトライ意図を取り出している。
したがって、終了トークンを作り直さずにResultチェーンへ移せる。
現在のtryブロックの本体を、次の形にする案を推奨する（未適用）。

```csharp
var begun = await begin.ExecuteAsync(session);
outcome = await begun.Result.StartAsync()
    .ThenAsync(async () =>
    {
        if (!await GameplayCheckpoint.WaitToContinueAsync(session, begun.End))
            return begun.Result;

        return (await sessionFlow.ExecuteAsync(session)).Result;
    });

retry = outcome.Resolve(
    () => session.Gameplay.ReadGameState(session.SessionId).State.RetryRequested,
    _ => false);
```

ThenAsyncはBeginの失敗時に後続を呼ばず、その失敗を保持する。
成功時にもGameplayCheckpointで停止・ポーズ・再開を判断する。
Resolveは失敗時のリトライ判定をfalseにし、状態の読み取りも成功時だけにする。
送出例外とTaskのキャンセルは現在の外側のcatchへ伝播し、既存の終了処理につながる。

EndSessionは、このチェーンの後で成功・失敗の両方について実行する。
先行結果をCleanupInfoで渡し、EndSessionのInteractionEndを返す現在の構成を維持する。
これにより「開始が成功したらセッションを進め、その結果からリトライを判断する」という部分を、
Resultの語彙で記述できる。

### WindowSystemFlow: Open成功時のゲーム起動とフレーム反復

既存の`failures.Add(opened.Result)`を残し、直後の成功判定を次の形にする案を推奨する。

```csharp
opened.Result.OnSuccess(() =>
{
    execution = Task.Run(() => game.ExecuteAsync(gameplay));
    RunFrameLoop(frames, controls, input, execution, failures);
});
```

この区間は成功時の副作用なのでOnSuccessが対応する。
フレーム反復の返却失敗は引き続きFailureCollectorへ集まり、送出例外は外側のcatchへ伝播する。
OnSuccessが返すのはOpenのResultであり、ゲーム実行・フレーム反復全体の結果ではない点を区別する。
StopAndJoinGameplayはfinallyに残す。同期OnSuccessはその場で実行されるため、
Open・フレーム反復・Closeの呼び出し元スレッドを維持できる。
ここでStartAsync/ThenAsyncを使うと、内部のConfigureAwait(false)によって継続スレッドが変わり得るため採用しない。

### GameSessionSystemFlow: ターン反復は維持する

各ターンは、失敗時または正常停止時にそのターンのInteractionEndを返し、
継続時だけ次の反復へ進む。HasExceptionと継続判断をGameplayCheckpointが既にまとめている。
ここはチェーン化による重複判定や終了トークン保存用の変数を増やさず、現在のwhileと早期returnを維持する。
SystemFlowのResult利用は、上記二つの区間から導入する。

検証: 既存のFlowTests / CleanupTests / FailureTestsで、Begin失敗時の打ち切り、
停止直後の起動、各ターン境界の失敗とキャンセル、リトライ、停止失敗後の合流・閉鎖を確認する。
追加で、Openが失敗Resultを返した場合にゲームを起動せず、停止・閉鎖へ進み、元の失敗を保持するケースと、
Beginが失敗Resultを返した場合にSessionを実行せず、EndSessionへその失敗を渡すケースを確認する。
既存テストに同じ契約の直接検証があれば重複追加は避ける。

## 改善案3: Interactionの段階接続にResultを使う（旧案）

この節は初回の設計記録。ContinueAfterAsyncの案は上記「Interactionの再検討と再修正」で置き換えた。

### 接続方法: ResolveAsyncで元のReactionEndを保持する

PlayTurn / HandleResultは複数のReactionを実行するため、成功時の次段階を連結する対象になる。
ただし、Reactionの返却型はResultではなくReactionEndなので、ResultだけをThenAsyncで連結すると、
失敗時に返すべき終了トークンが失われる。
また、正常停止も成功Resultなので、Resultの成功だけで次段階へ進めてはならない。

既存のGameplayCheckpointに次の補助を追加する案を推奨する（未適用）。
Result拡張のusingとして`InteractionFlow.Core.Entities`を追加する。

```csharp
internal static Task<ReactionEnd> ContinueAfterAsync(
    IGameplayContext context,
    ReactionEnd end,
    Func<ValueTask<ReactionEnd>> next)
    => end.Result.StartAsync().ResolveAsync(
        async () =>
        {
            if (!await WaitToContinueAsync(context, end))
                return end;

            return await next();
        },
        _ => Task.FromResult(end));
```

失敗時はチェックポイントとnextを呼ばず、元のReactionEndを返す。
成功時は停止・再開の確認を経て次段階へ進み、正常停止なら元のReactionEndを返す。
コールバックや待機から送出された例外・キャンセルは捕捉せず、Interaction基底のPort委譲へ伝播する。
引数は実行済みTaskではなく関数なので、失敗や停止の判定前に次段階を開始しない。
ReactionEndの生成はReactionだけが担い、この補助は既存の終了トークンを選んで返す。

これはResultのResolveAsyncを使った段階接続であり、成功・失敗の両経路からReactionEndへ戻せる。
ThenAsyncの利用は、次もResultを返す局所処理に適用する。
失敗したReactionEndを単にAsResultで包むと外側が成功になるため、その方法は採用しない。
補助はゲーム固有の停止・再開方針を含むため、Coreの汎用Result APIには追加しない。

### HandleResult: 結果提示 → 入力受付 → リトライ待機

ExecuteCoreAsyncは次の形にできる。
WaitForRetryAsyncは現在のwhileをprivateメソッドへ移したものとする。

```csharp
var shown = await reaction.PresentResultAsync(context);
return await GameplayCheckpoint.ContinueAfterAsync(context, shown, async () =>
{
    var enabled = await reaction.EnableResultInputAsync(context);
    return await GameplayCheckpoint.ContinueAfterAsync(
        context, enabled, () => WaitForRetryAsync(context, enabled));
});
```

WaitForRetryAsyncは`ValueTask<ReactionEnd>`を返し、現在のReadCommand → HandleCommand →
WaitToContinueAsyncの反復を保つ。RetryRequestedが既にtrueなら引数のenabledを返す。
これで提示失敗時に入力受付を開かず、受付失敗時にReadCommandを呼ばないことを接続方法で表せる。
非同期処理中に正常停止した場合も次段階へ進まない。

### PlayTurn: 選択開始 → 選択・確定 → プレイヤー行動 → 敵行動

選択開始後の処理をSelectAndActAsyncへまとめ、冒頭を次の接続にする。

```csharp
var selected = await reaction.BeginActionSelectionAsync(context);
return await GameplayCheckpoint.ContinueAfterAsync(
    context, selected, () => SelectAndActAsync(context, selected));
```

SelectAndActAsyncは`ValueTask<ReactionEnd>`を返し、現在のConfirmedまでの入力反復を保つ。
各コマンド後のWaitToContinueAsyncと早期returnも保ち、ループを抜けた時点で
プレイヤー行動を実行する。そこから敵行動への接続を次の形にする。

```csharp
var applied = await reaction.ApplyPlayerActionAsync(context);
return await GameplayCheckpoint.ContinueAfterAsync(context, applied, () =>
    context.Gameplay.ReadGameState(context.SessionId).State.Outcome != GameOutcome.Running
        ? new ValueTask<ReactionEnd>(applied)
        : reaction.ApplyEnemyActionAsync(context));
```

プレイヤー行動の失敗時は勝敗を読まず、敵行動を実行しない。
成功時は従来と同じくチェックポイントの後で勝敗を読み、勝敗確定済みならappliedを返す。
敵行動の後には新しいチェックポイントを加えず、その終了結果を返す既存の意味を保つ。
入力反復の各段階までネストしたラムダへ移す必要はない。

### 残りのInteraction

| 対象 | 再検討結果 |
| --- | --- |
| BeginSession / HandleControlInput / OpenWindow / RequestShutdown | 単一Reactionへの委譲を維持する。接続する次段階がない。 |
| EndSession / CloseWindow | CleanupInfoの種別検証と単一Reactionへの委譲を維持する。先行失敗があっても終了Reactionを実行するので、PreviousResult.Thenで接続しない。 |
| PresentFrame | voidの入力取得とboolの受理判定は現在の契約を維持する。終了・ポーズ・スキップの優先順位を保ち、受理されなくても描画する。Result.Successから始める装飾的なチェーンは加えない。 |

これらのPortが将来、入力取得などの局所失敗をResultで返す必要を持った場合には、
そのResultから次段階へのThen/Resolveによる接続を追加で検討する。
今回の段階接続のためにPort契約を変更する必要はない。

### 実装時の検証

- 失敗Resultを返すReaction、例外を送出するReaction、キャンセルされる非同期処理を区別する。
- BeginActionSelection / HandleCommand / ApplyPlayerActionの失敗で後続処理を呼ばないことを確認する。
- PresentResult / EnableResultInputの失敗で入力受付・コマンド取得が進まないことを確認する。
- 失敗時には既存のReactionEndに保持されたResultExceptionを同一のまま返すことを確認する。
- 正常停止・ポーズと再開・勝敗確定・リトライ・確定済み状態の保持を既存ケースで確認する。

FlowTestsのReturnedTurnFailureDoesNotRunTheNextStep、ResultInteractionTestsの提示失敗・取消、
GameplayReactionTestsの再開・停止・確定済み状態のテストを活用する。
EnableResultInputの「失敗Result返却」は現在の送出例外ケースとは別に確認する。
継続補助の内部呼び出し回数を固定するテストではなく、後続Portが呼ばれないことと返却結果を検証する。
新しいTask・ラムダの割り当てが加わるため、フレームごとのPresentFrameにはこの非同期補助を適用しない。
提示やユーザー入力を待つPlayTurn / HandleResultへの適用とし、性能問題があれば既存計測で判断する。

## 改善案4: セッション確保と停止の競合を値で表す（次点）

対象: [GamePlaySystemFlow](../../InteractionFlow.Samples.EscapeGame/SystemFlows/GamePlaySystemFlow.cs)、
IGameplayAccess.AllocateSession、EscapeAccess.AllocateSession。

現在は停止後のAllocateSessionがInvalidOperationExceptionを投げ、
GamePlaySystemFlowがShutdownRequestedを条件に正常終了へ変換する。
既存テストで停止直後の起動は検証されているが、通常の競合を例外型と停止フラグの組み合わせで識別している。
try範囲にはGameplayContextの生成も含まれ、停止競合以外のInvalidOperationExceptionまで同じ扱いになり得る。
これはコード構造上の懸念であり、現行実装で誤判定が発生したという確認ではない。

まず`TryAllocateSession(out long id)`のように、確保と停止判定を同じlock内で値として返す案を検討する。
将来、停止以外の回復可能な失敗も契約として必要になった場合には、
`Result<SessionAllocation>`とし、成功値にAllocated/Stoppedを持たせる方法もある。
停止を失敗Resultとして作ってからThenErrorで無条件に成功へ戻す設計は避ける。
単にResult利用を増やすためだけにResultと専用コンテナーを追加する必要はない。

検証: 停止前は一意なIDを確保でき、停止後は確保せず、競合時も旧セッションの終了とウィンドウ閉鎖を完了すること。
既存のShutdownBeforeWorkerStartsIsSuccessfulを維持し、確保の契約変更に対応するAccessテストを更新する。

## 改善案5: 送出例外をResultへ変換する境界を明文化する

対象: [WindowSystemFlow](../../InteractionFlow.Samples.EscapeGame/SystemFlows/WindowSystemFlow.cs)、
GamePlaySystemFlow、RaylibWindowReaction、GameExceptionHandling。

WindowSystemFlowとGamePlaySystemFlowは、子の失敗Resultに加えて送出例外も捕捉し、
後始末へ渡すResultに変換している。Interactionは既存の例外・キャンセルPortも利用する。
一方、RaylibWindowReaction.Open/DrawFrameの外部API例外は通常、Interactionの例外Portまで伝播する。

Result APIの原則は「Resultを返す委譲先の例外を呼び出し側で勝手にResult化しない」である。
現行のフローが実行寿命の所有者として例外を最終結果へ取り込む意図を、通常の委譲と区別して記録する必要がある。
この点は契約の整理候補であり、catchを単純削除する変更は勧めない。

- 外部API操作の失敗を局所Resultとして返すなら、担当External内でその操作だけを捕捉し、GetEndへ渡す。
- Context取得・スレッド検証・他のResult返却処理まで含む包括catchを新設しない。
- フロー所有者が送出例外を最終Resultへ取り込む方針を維持するなら、その例外境界を明示する。
  伝播を維持する方針へ変える場合は、後始末後に元の例外と追加失敗を保持して再送出する設計が必要になる。
- ThenErrorは送出例外やキャンセルTaskを捕捉しないため、既存catchの代用にしない。

検証: 返却された失敗・送出例外・Taskのキャンセルを別々に扱い、
停止要求、ゲーム合流、セッション終了、ウィンドウ閉鎖の実行と先行失敗の保持を確認する。
実raylib境界を変更する場合は、描画代替テストに加えて実ウィンドウで確認する。

## 維持する設計

- 終了結果の生成はReactionが担う。後始末へ渡すResultを終了トークンから取り出して連結することはできるが、
  ResultからSystemFlowが独自に終了トークンを作り直さない。返す必要がある終了トークンは保持する。
- GameplayCheckpointのboolは、成功に加えてStopping・ポーズ・再開待機を含む継続判断である。
  Result.Successから自動的に「継続可能」と判断しない。
- FailureCollectorは全失敗の集約を担う。Thenで置き換えると先行失敗時に後始末が省略される。
- 入力拒否、ゲーム内の無効操作、Deferred、Stoppedを一律に失敗Resultへ変えない。
- ReadCommandAsyncの現在の実装はAccessの入力待機へ委譲している。
  局所的な失敗値の要件がないままResult<GameCommand>へ変更せず、Taskキャンセルとの区別を保つ。
- raylibのOpenからCloseまでを呼び出し元スレッドで完了する同期フレームループを保つ。

## 調査時の検証結果

既存のEscapeGameテストを次のコマンドで実行し、135件成功、失敗・スキップ0件を確認した。
依存プロジェクトを含む対象テストのビルドも成功した。

```text
dotnet test InteractionFlow.Samples.EscapeGame.Tests/InteractionFlow.Samples.EscapeGame.Tests.csproj --no-restore
```

当初の調査ではこの文書だけを追加し、全体ソリューションの再ビルド・再テストと実GUI操作は実施していない。
既存の作業ツリーにある変更を含む結果であり、任意の実行順序や実raylibの障害を網羅する保証ではない。

実装後の確認結果は冒頭の実装結果と下の記録を参照する。
Core/Standardに新APIを追加する必要はなかった。

## 実装後の検証結果

- `dotnet build InteractionFlow.slnx`: 警告0、エラー0。
- 初回の`dotnet test InteractionFlow.slnx`: 全611件成功、失敗・スキップ0。EscapeGameは141件、他470件。
- `dotnet build benchmarks/InteractionFlow.ExecutionMeasurements/InteractionFlow.ExecutionMeasurements.csproj --no-restore`:
  警告0、エラー0。ソリューション外のセッション確保API利用箇所も確認した。
- `git diff --check`: 問題なし。

サンプルの進行確認は、描画を代替した既存のFlowTestsで12ターンの解法・結果提示・リトライ、
ポーズと再開、停止とキャンセル、合流と閉鎖を実行して確認した。
実ウィンドウとキーボード操作の再確認、ゲーム全体の性能再計測は実施していない。
フレームごとのPresentFrameへ非同期チェーンを追加せず、ネイティブ描画の呼び出し順序も変更していない。

Result<ReactionEnd>への再修正後も全体ビルドは警告0・エラー0。
`dotnet test InteractionFlow.slnx --no-restore`は全613件成功、失敗・スキップ0（EscapeGame143件、他470件）。
結果提示に加えて入力受付後の正常停止・キャンセルを確認する2ケースを追加し、
失敗と正常停止のいずれでも後続の入力を読まないことを確認した。
元の終了トークンの同一性、返却されたキャンセル失敗とTaskキャンセルの区別、
ターン各段階の失敗、勝敗・リトライ・ポーズ・再開は既存ケースも含めて確認した。
再修正後の実GUI操作と性能の再計測は行っていない。

HandleResultのチェーン一本化後も全体ビルドは警告0・エラー0。
`dotnet test InteractionFlow.slnx --no-restore`は全614件成功（EscapeGame144件、他470件）。
入力待機後のHandleCommandが失敗Resultを返すケースを追加し、元のReactionEndを保持して終了し、
リトライを確定せず入力待機も残さないことを確認した。

PlayTurn / HandleResultを変更前へ戻した後も、全体ビルドは警告0・エラー0、
全体テストは614件成功（EscapeGame144件、他470件）。両ソースにHEADとの差分がないことを確認した。

SystemFlowの実行境界と終了処理の再整理後も、全体ビルドは警告0・エラー0。
`dotnet test InteractionFlow.slnx --no-restore`は全621件成功（EscapeGame151件、他470件）、失敗・スキップ0。
描画を代替したフローでリトライ・停止・再開・失敗時の合流と閉鎖を確認し、
追加ケースではフロー境界へ伝播する例外と終了時の追加例外、描画の呼び出しスレッドも確認した。
実GUI操作と性能再計測は行っていない。

SystemFlowのチェーン化を撤回した最終状態でも、全体ビルドは警告0・エラー0、
全体テストは621件成功（EscapeGame151件、他470件）、失敗・スキップ0。
WindowSystemFlowと二つのInteractionにHEADとの差分はなく、
GamePlaySystemFlowはTryAllocateSessionへの対応だけを残した。
