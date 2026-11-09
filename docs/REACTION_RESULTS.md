# Reaction とフローの結果

相互作用（Interaction、またはその組み合わせの SystemFlow）は、明示的なリアクションによって動作を終了します。
処理の成功・失敗と、この終了結果の生成規則を分けて扱います。

| 型 | 責務 |
| --- | --- |
| Result | 局所的な処理の成功・失敗と連結 |
| ReactionEnd | Result を保持し、Reaction による生成規則を付与 |
| InteractionEnd | ReactionEnd に基づく Interaction / SystemFlow の完了 |

## 結果の生成と利用

Reaction / IReactionPort の実装から protected GetEnd(Result) で生成します。
GetEnd() は成功、GetEnd(Exception?) は null なら成功、例外があれば失敗 Result に変換します。
既存の失敗 Result は、その ResultException インスタンスを保持します。
ReactionEnd のコンストラクターは internal のままで、Result からの公開変換はありません。

```csharp
// Reaction 派生型内
return GetEnd(operationResult);

// Reaction の呼び出し側
var end = await output.WriteLineAsync(context, text);
if (end.HasException) return end; // 失敗した Reaction の結果で終了し、入力・保存へ進まない
```

単純な失敗判定には HasException を使い、連結・回復・失敗の取得には Result を使います。
サンプルの Interaction が直接受け取った Reaction の失敗は、中断のために例外化せず、その結果を返します。
Notepad のキー取得補助は相互作用の終了境界ではなく、Task<Result<ReadKeyValue>> を返す取得処理です。
成功値は取得済みキー、または中断・候補なし・不正 ID の状況を表します。
取得処理の失敗は外側の Result に保持し、プロンプトの失敗 Result も再ラップしません。
取得側はプロンプトと選択表示を担当し、最終的な診断の出力は呼び出し側の Interaction が担当します。
取得失敗時は呼び出し側が明示的に ExceptionPort に委譲し、例外を送出せず ReactionEnd を取得します。
実際に送出された例外は、従来どおり Interaction の処理 Port に委譲します。
Notepad の選択・下書き編集 Operation も Result を返します。
SelectAsync は Result<ChoiceValue>、EditAsync は Result<NoteEditValue> です。
選択中断は ChoiceValue.Index が null、編集破棄は NoteEditValue.Note が null の成功結果で表します。
不正な選択入力は従来どおり Index = -1 で区別します。
表示・画面クリア・カーソル移動・端末サイズ取得が返した失敗は Result のまま伝播し、
呼び出し側の Interaction が明示的に ExceptionPort へ委譲します。
画面クリアに失敗した場合は選択確定・編集完了の成功値を返しません。
Operation や委譲先の送出例外を包括的に捕捉する処理は追加しません。
Task のキャンセルは失敗 Result へ変換せず伝播し、Interaction のキャンセル処理につながります。
失敗 Result に格納された OperationCanceledException は Task のキャンセルとは区別します。

ReactionEnd.Result と InteractionEnd.Result は既存の Result の契約に従います。
返された失敗、送出例外、キャンセルされた Task を混同しません。
Result の連結 API に、委譲先の例外を自動捕捉する動作は追加しません。

補助プロパティ HasException は Result の失敗を表します。
Exception は ResultException の一段のラッパーを除いた原因を返します。
内部例外がなければ ResultException 自体を返します。
HasCanceled / CanceledException はその直接の原因が OperationCanceledException の場合に反応します。
集約例外の中にキャンセルがあっても、集約全体をキャンセルとは判定しません。
キャンセル処理の成功は成功 Result であり、ユーザーの終了意図とは別です。

## InteractionEnd の役割と移行

LastContext は削除しました。Context は引数として渡し、寿命は呼び出し側が管理します。
IInteraction の実装では GetEnd(context, reactionEnd) を GetEnd(reactionEnd) に置き換えます。
Interaction 基底クラスを利用する場合は、この変更は内部で処理されます。
SystemFlow の GetEnd(context, token) は削除し、子から返された token を直接返します。
SystemFlow 基底クラスも同じトークンを返し、Context の結合やトークンの再生成を行いません。

InteractionEnd は Reaction の完了と、単独または SystemFlow で合成された相互作用の完了を区別します。
ここでの Interaction はクラス名だけでなく相互作用を意味します。
旧名は FlowEndToken です。Context 保持を廃止した後の責務に合わせて改名しました。
SystemFlowEnd は追加しません。SystemFlow 固有の終了契約が必要になった時点で分離を検討します。
今回の実装で追加された実行識別子や履歴はなく、過去の結果の再利用まで禁止するものではありません。
トークンが返されたこと自体はアプリ全体の終了指示ではなく、進行は SystemFlow が判断します。

## 型付き Interaction の実行契約

`IInteraction<TContext>` / `Interaction<TContext>` は、実行に必要な Context 型を表します。
`ExecuteCoreAsync(TContext)` と `OnCancellation(TContext)` に、呼び出し元が渡した型付きの
Context が届きます。ReactionEnd / InteractionEnd の生成規則と、例外・キャンセル処理は共通です。

従来の `IInteraction` / `Interaction` は、任意の `IFlowContext` で実行する契約として残します。
専用 Context を要求する型は `IInteraction` を実装せず、共通のメタデータは
`IInteractionNode` を通じて扱います。メタデータだけから実行可能な Context 型は判断できません。

`UseInteraction<TImplementation>()` は両方を登録できます。
型付き版も汎用版も、同じ実装で Task 登録・解除、キャンセル時の後始末、処理 Port への委譲を行います。
Context の所有権や Interaction の直接ネストに関する契約は変えません。
型の関係と反変の扱いは [型付き Interaction の契約](./design-notes/TYPED_INTERACTION_CONTEXT.md) を参照してください。

同期成功時は内部のTaskラッパーを介さず結果を処理しますが、本体開始前のキャンセル登録と
後始末を含む通知完了・登録解除の契約は共通です。
計測と改善範囲は [Interaction実行の性能計画](./design-notes/INTERACTION_EXECUTION_PERFORMANCE_PLAN.md) を参照してください。

Interaction の本体契約 `ExecuteCoreAsync` は `ValueTask<ReactionEnd>` を返します。
同期処理は `new(reactionEnd)` で結果を返し、非同期処理は `async ValueTask<ReactionEnd>` で実装できます。
既存 Task を包む場合は `new ValueTask<ReactionEnd>(task)` を使用できます。
これは派生実装の override に対する戻り値型の変更です。公開の `ExecuteAsync` は
`Task<InteractionEnd>` を維持し、直接interface実装と呼び出し側の契約は変更していません。
基底は本体の ValueTask を一度だけ消費します。複数回の await や AsTask 変換は行いません。
