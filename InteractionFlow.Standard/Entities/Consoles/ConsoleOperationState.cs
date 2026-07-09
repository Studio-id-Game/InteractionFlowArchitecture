using System;

namespace InteractionFlow.Standard.Entities.Consoles
{
    /// <summary>
    /// コンソール入力操作で使用する状態を表します。
    /// </summary>
    /// <param name="backgroundColor">背景色。</param>
    /// <param name="foregroundColor">前景色。</param>
    /// <param name="writeLine">入力後に改行するかどうか。</param>
    /// <param name="cancelWaitTime">入力終了後にキャンセル検知を待つ時間。</param>
    public class ConsoleOperationState(ConsoleColor backgroundColor, ConsoleColor foregroundColor, bool writeLine, int cancelWaitTime)
        : ConsoleState(backgroundColor, foregroundColor, writeLine), IFunctionState<ConsoleOperationState>
    {
        /// <summary>
        /// 標準のコンソール入力状態を取得します。
        /// </summary>
        public static new ConsoleOperationState Default => new(ConsoleState.Default, 100);

        /// <summary>
        /// 改行しない標準のコンソール入力状態を取得します。
        /// </summary>
        public static new ConsoleOperationState DefaultNoLine => new(ConsoleState.DefaultNoLine, 100);

        /// <summary>
        /// コンソール出力状態とキャンセル待機時間から入力状態を作成します。
        /// </summary>
        /// <param name="state">元にするコンソール状態。</param>
        /// <param name="cancelWaitTime">入力終了後にキャンセル検知を待つ時間。</param>
        public ConsoleOperationState(ConsoleState state, int cancelWaitTime) : this(state.backgroundColor, state.foregroundColor, state.writeLine, cancelWaitTime)
        {

        }

        /// <summary>
        /// 入力終了後にキャンセル検知を待つ時間を保持します。
        /// </summary>
        public int cancelWaitTime = cancelWaitTime;

        /// <summary>
        /// 色と改行有無を <see cref="ConsoleState"/> として取得または設定します。
        /// </summary>
        public ConsoleState ConsoleState
        {
            get => this;
            set
            {
                backgroundColor = value.backgroundColor;
                foregroundColor = value.foregroundColor;
                writeLine = value.writeLine;
            }
        }

        /// <summary>
        /// 指定された項目だけを現在の入力状態へ反映します。
        /// </summary>
        /// <param name="foregroundColor">変更する前景色。</param>
        /// <param name="backgroundColor">変更する背景色。</param>
        /// <param name="writeLine">変更する改行有無。</param>
        /// <param name="cancelWaitTime">変更するキャンセル待機時間。</param>
        public void Update(ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null, bool? writeLine = null, int? cancelWaitTime = null)
        {
            ConsoleState.Update(foregroundColor, backgroundColor, writeLine);

            if (cancelWaitTime != null)
                this.cancelWaitTime = cancelWaitTime.Value;
        }

        /// <summary>
        /// 現在の入力状態をコピーします。
        /// </summary>
        /// <returns>現在の状態と同じ内容を持つコピー。</returns>
        public new ConsoleOperationState Copy()
        {
            return new(backgroundColor, foregroundColor, writeLine, cancelWaitTime);
        }
    }
}
