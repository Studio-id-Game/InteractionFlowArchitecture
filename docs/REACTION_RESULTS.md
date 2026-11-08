# Reaction とフローの結果

相互作用（Interaction、またはその組み合わせの SystemFlow）は、明示的なリアクションによって動作を終了します。
処理の成功・失敗と、この終了結果の生成規則を分けて扱います。

| 型 | 責務 |
| --- | --- |
| Result | 局所的な処理の成功・失敗と連結 |
| ReactionEnd | Result を保持し、Reaction による生成規則を付与 |
| FlowEndToken | ReactionEnd に基づく Interaction / SystemFlow の完了 |

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
end.Result.ThrowIfError(); // 失敗時に入力・保存へ進ませない明示的な境界
```

ReactionEnd.Result と FlowEndToken.Result は既存の Result の契約に従います。
返された失敗、送出例外、キャンセルされた Task を混同しません。
Result の連結 API に、委譲先の例外を自動捕捉する動作は追加しません。

補助プロパティ HasException は Result の失敗を表します。
Exception は ResultException の一段のラッパーを除いた原因を返します。
内部例外がなければ ResultException 自体を返します。
HasCanceled / CanceledException はその直接の原因が OperationCanceledException の場合に反応します。
集約例外の中にキャンセルがあっても、集約全体をキャンセルとは判定しません。
キャンセル処理の成功は成功 Result であり、ユーザーの終了意図とは別です。

## FlowEndToken の役割と移行

LastContext は削除しました。Context は引数として渡し、寿命は呼び出し側が管理します。
IInteraction の実装では GetEnd(context, reactionEnd) を GetEnd(reactionEnd) に置き換えます。
Interaction 基底クラスを利用する場合は、この変更は内部で処理されます。
SystemFlow の GetEnd(context, token) は削除し、子から返された token を直接返します。
SystemFlow 基底クラスも同じトークンを返し、Context の結合やトークンの再生成を行いません。

FlowEndToken は今回は維持し、Reaction の完了と Interaction / SystemFlow の完了を区別します。
今回の実装で追加された実行識別子や履歴はなく、過去の結果の再利用まで禁止するものではありません。
トークンが返されたこと自体はアプリ全体の終了指示ではなく、進行は SystemFlow が判断します。
