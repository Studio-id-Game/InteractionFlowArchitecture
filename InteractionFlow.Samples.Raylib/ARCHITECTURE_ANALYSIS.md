# Raylib サンプルとアーキテクチャの整合性

この文書は、現在の Raylib サンプルを次の三文書と照合した分析です。
サンプル固有の設計判断と留保を記録するものであり、ライブラリ全体の契約を追加・変更しません。

- [Philosophy](../docs/PHILOSOPHY.md)
- [計算モデルとして](../docs/COMPUTATIONAL_MODEL.md)
- [ライブラリ実装](../docs/LIBRARY_IMPLEMENTATION.md)

基本構造は三文書に整合し、照合した範囲で明確な API 契約・依存方向の違反は見当たりません。
ただし、終了時の文脈更新と、Reaction 完了から画面提示までの関係には設計上の留保があります。
この評価はコードと文書の照合によるもので、理論モデルへの形式的な適合証明ではありません。

## Philosophy との対応

このサンプルでは、ユーザーの体験を次の流れとして記述しています。

```text
入力待ち → 操作の受理 → 処理中 → カウント更新と演出 → 次の入力待ち
```

処理中は追加入力を受け付けず、キャンセル後は再入力でき、終了要求後は受付へ復帰しません。
この流れを State の規則、Operation、Reaction、SystemFlow に分担させる構成は、
Philosophy の「コードの構造と、ユーザーが経験する構造を近づける」という方針に沿っています。

`CounterFlowContext` が提供するアクセス契約はシステム側の状態を読み書きする手段ですが、
それ自体が Context 全体や Context Loop ではありません。
画面の変化、ユーザーによる認識と次の操作、システムの反応が続く時間的な過程が Context Loop です。
過去の相互作用を物理的な追記ログとして保存することは、このサンプルの成立条件ではありません。

## 計算モデルとの対応

| 計算モデル上の役割 | サンプルでの対応 |
| --- | --- |
| Operation による入力 | `RaylibWindowOperation.Poll`、`CounterOperation.Submit` / `ReadAsync` |
| Reaction による応答 | カウント・表示の更新、毎フレームの描画、終了通知 |
| Domain の遷移規則 | `CounterState` の受理条件、開始・完了・中断・失敗・終了の規則 |
| System Flow の合成と選択 | ウィンドウの起動、フレーム反復、カウンター実行、停止と終了 |
| FunctionState | `RaylibWindowReaction.ViewState` の前回カウントと演出の減衰状態 |

`ViewState` は描画を加工する履歴・演出状態であり、
計算モデルの「システム全体のテープ構成と Function の役割」で挙げる FunctionState に対応します。
ゲーム進行のために CounterState へ集約する必要はなく、Reaction 内部に保持できます。

一つの C# オブジェクトと、計算モデル上の分類は一対一対応ではありません。
このサンプルでは、ゲームの状態と規則を CounterState、表示を CounterDisplayState、
入力待機の補助状態を CounterInputWait に分け、CounterAccess が同じ同期境界で扱います。
Entities 配下にあることや Context から参照されることだけで、全体を ContextTape、DomainState、
FlowState のいずれか一つと同一視しません。特に `CounterState.Phase` はカウンターの段階であり、
System Flow の評価位置を表す FlowState そのものではありません。

Storage と SilentExternal は、このサンプルには導入していません。
独立した記録を目的とする機能はなく、ウィンドウの生成・描画・消失はユーザーへの反応として扱います。
計算モデルは、Interaction がすべての Function 分類を含むことを要求していません。

## ライブラリ実装との対応

| 要素 | このサンプルでの責務 |
| --- | --- |
| Program / Builder / Handler | 実行環境の構築、実行開始、Scope と Context の寿命管理 |
| SystemFlow | Interaction の合成、並行実行の調整、継続・終了の判断 |
| Interaction | Port を使った一段階の実行手順 |
| ExternalPorts | 操作取得・反応の利用契約 |
| Externals | raylib や非同期入力への接続 |
| Entities | 外部ライブラリに依存しない状態と遷移規則 |

薄い `CounterOperation` も、専用 Context の Input プロパティから入力アクセス契約を取得し、
操作の取得・送信を差し替え可能な Port として提供する役割があります。
受付規則を Operation 自身に複製する必要はありません。

各 Interaction は明示 Reaction の結果によって完了します。
Reaction が生成した ReactionEnd を Interaction が受け取り、
SystemFlow は InteractionEnd を継続・終了判断に利用します。
Interaction の完了を、そのままアプリ全体の終了指示とは扱いません。

## Context によるアクセス管理と State の分割

`CounterFlowContext` は値を取得する入口であり、実行制御やゲームの状態遷移を担当しません。
Input / State / Display の型付きプロパティを提供し、`TryGetCore` も同じプロパティへ委譲します。
各プロパティは Context 自身の破棄を確認し、取得済みアクセス契約の共有寿命とは区別します。
専用の Port・Interaction・SystemFlow は CounterFlowContext 型を維持し、例外・キャンセル Handler は
共通の IFlowContext 契約からアクセス値を取得します。
`CounterAccess` が複数状態への一貫したアクセス範囲を保護し、State は個別の値と規則に集中します。

固定プロパティと追加値の整合性は `ValidateAdditionalValue` で保ちます。
3種類のアクセス契約を実装する値と、すべての Entry<T> 派生値を追加時に拒否します。
Entry を現在の解決結果だけで許可すると、後日の値変更で固定契約を隠蔽できるため、
Entry 全体を禁止するのがこのサンプルの明示的な方針です。Core 共通の上書き禁止規則ではありません。
通常の FlowContext を子にした場合はその型の規則に従い、親のアクセス契約を隠蔽できます。

この分割は、State ごとに独立したロックを持つ分割ではありません。
受付条件、通知、処理段階、表示などの関連する更新は同じ gate で行います。
カウンターの生の可変 State を Context から取得・持ち出しできないため、取得後の直接更新で同期を迂回しません。
メインスレッドで扱う FrameState / WindowState は、この保護対象とは別にフレーム用 Context から取得できます。
一方、非同期待機の間はロックを外し、完了反映時に終了意図などを再確認します。

独立 Context 間ではアクセス管理だけを共有し、キャンセル状態と追加値は共有しません。
型付きの親あり CounterFlowContext はアクセス管理とキャンセルを共有し、通常の親探索を維持します。
With が返す型は FlowContext のままなので、専用 Context の宣言と追加値の登録は分けます。
Context の破棄は共有状態を破棄せず、Task の停止・待機もしません。
この仕組みはサンプル固有の実装であり、ライブラリ全体の並行安全性を追加保証するものではありません。
以下の終了時の文脈更新と描画完了に関する留保も、この構成変更だけでは解消しません。

## 留保 1：終了処理からの文脈更新

[WindowSystemFlow.StopCounter](./SystemFlows/WindowSystemFlow.cs) は、
`ICounterStateAccess.RequestExit` を直接呼びます。
アクセス管理は CounterState の終了意図・段階と CounterDisplayState のメッセージを協調更新します。
失敗状態でなければ表示も変わるため、アクセス契約を経由してもこの例外は解消していません。
したがって、単なる内部的な待機制御に限らず、ユーザー向けの文脈更新も含みます。

ライブラリ実装文書の「Context 更新の原則」は、そのような更新を Operation / Reaction 内で行い、
観測可能な反応と対応させることを理想としています。この経路は、その理想から外れる部分です。
一方、同文書はこの原則を型・ライブラリによる強制契約とはしていません。

このサンプルでは、正常終了・OS の終了要求・失敗のいずれでも受付を閉じ、
キャンセルのリセット後に再開しないようにする終了調整上の例外として扱います。
原則へさらに寄せる場合は、終了要求の適用を Operation / Reaction へ委譲する余地があります。

なお、終了用 Context に集約 Result を追加することは、終了処理へ渡す技術的な入力の構成です。
Context に値を追加したという事実だけで、ユーザーとの共有文脈の更新と同一には扱いません。

## 留保 2：Reaction 完了と画面提示は別

[CounterReaction](./Externals/CounterReaction.cs) は State を更新すると終了結果を返します。
実際の描画は、別のフレーム Interaction が [RaylibWindowReaction](./Externals/RaylibWindowReaction.cs) を
呼び出すことで行います。このため、Counter 側の個々の Interaction 完了と、対応する画面提示は一対一ではありません。

特にカウンターのフローが終了するとフレーム反復も終了するため、
Goodbye などの終了直前の表示は、一度も描画されない可能性があります。
CounterReaction の完了は状態更新まで、描画 Reaction の完了は EndDrawing までです。
いずれも、ユーザーが実際に認識したことを保証するものではありません。

計算モデルの「理想的な Interaction と状態変化」が述べる Reaction の観測という理想を、
個々の Counter Interaction 単位で保証している構成ではありません。
ただし、同節はその観測をライブラリが保証しないことも明記しています。
これは API 契約違反ではなく、このサンプルが採用する完了範囲です。

今後、個々の表示や演出の提示完了を待つ仕様を導入する場合は、この境界を再検討します。
その際は、メインスレッドがカウンター終了を待ち、カウンター側が描画完了を待つ循環待機にも注意が必要です。

## 並行実行と検証の範囲

フレームとカウンターは並行して実行しますが、それ自体を計算モデルとの矛盾とは評価しません。
計算モデルの「状態遷移演算子としての Interaction」は、論理的に順序付けた一つの実行トレースと、
実装のスレッド安全性・スケジューリングの再現性を区別しています。

このサンプルでは、分割 State を扱うアクセス管理の共通ロック、フレーム用の独立した CancellationObject、
メインスレッドに限定した raylib 呼び出しによって実装上の整合性を保ちます。
これらは、同じ初期値から同じ実スケジューリングを再現することや、
並行した Interaction 全体を単純な逐次マクロ遷移として形式的に検証したことを意味しません。
時刻、入力、キャンセル、スケジューリングの差を含めた実行の再現性は別の検証対象です。

チューリング完全性についても、この小さなカウンター自体が文書の十分条件を実証するものではありません。
ここで確認しているのは、概念・責務・API 利用の対応と、理想原則からの留保です。

State の遷移とフローのテストは [関連テスト](../InteractionFlow.Samples.Raylib.Tests/) を参照してください。
それらの成功と、ユーザーによる反応の認識や三文書への形式的適合の証明は区別します。

## API 検証から見つかった検討事項

専用 Context のアクセス契約をプロパティとして公開し、Port も同じ Context 型を要求する検証から、
Core に `Interaction<TContext>` と、Context 型ごとの追加値検証フックが追加されました。
現在のサンプルは `Interaction<CounterFlowContext>` と型付き Port を使い、
専用 Context の型を SystemFlow から Operation / Reaction まで保っています。
追加値の検証は CounterFlowContext 自身が定義し、通常の子 Context へ制約を伝播させません。
背景と適用範囲は [Interaction の Context 型を引き継ぐ契約](../docs/design-notes/TYPED_INTERACTION_CONTEXT.md)、
[追加値バリデーター](../docs/design-notes/CONTEXT_ADDITIONAL_VALUE_VALIDATION.md) に記録しています。
