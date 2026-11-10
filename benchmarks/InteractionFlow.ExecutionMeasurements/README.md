# Interaction execution measurements

同期成功時の割り当て内訳と、制御された await 中断経路を調べる簡易計測です。
実行方法・測定値・制限・改善計画は
[Interaction 実行の計測と改善計画](../../docs/design-notes/INTERACTION_EXECUTION_PERFORMANCE_PLAN.md) を参照してください。

ライブラリの最適化実装ではありません。通常のソリューションビルドには追加していません。

EscapeGameの不変ゲーム状態の取得と、取得2回・ローカル時計更新を行う描画なしの入力待ち相当フレームも計測します。
計測用Reactionは状態参照と時計だけを保持し、実描画・表示文言生成・折り返しは含みません。
結果と対象外のコストは [EscapeGameの確認結果](../../InteractionFlow.Samples.EscapeGame/README.md) を参照してください。
演出待ちや実描画を含むゲーム全体の負荷計測ではありません。
