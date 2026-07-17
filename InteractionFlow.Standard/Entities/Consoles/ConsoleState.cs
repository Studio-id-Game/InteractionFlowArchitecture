using InteractionFlow.Core.Entities.Architectures;
using System;

namespace InteractionFlow.Standard.Entities.Consoles
{
    /// <summary>
    /// コンソール出力で使用する色と改行有無の状態を表します。
    /// </summary>
    /// <param name="backgroundColor">背景色。</param>
    /// <param name="foregroundColor">前景色。</param>
    /// <param name="writeLine">出力後に改行するかどうか。</param>
    public class ConsoleState(ConsoleColor backgroundColor, ConsoleColor foregroundColor, bool writeLine) : IFunctionState<ConsoleState>
    {
        /// <summary>
        /// 色セットと改行有無を指定して状態を作成します。
        /// </summary>
        /// <param name="colorSet">使用する色セット。</param>
        /// <param name="writeLine">出力後に改行するかどうか。</param>
        public ConsoleState(ConsoleColorSet colorSet, bool writeLine) : this(colorSet.Background, colorSet.Foreground, writeLine)
        {

        }

        /// <summary>
        /// 標準のコンソール状態を取得します。
        /// </summary>
        public static ConsoleState Default => new(ConsoleColorSet.Default, true);

        /// <summary>
        /// 改行しない標準のコンソール状態を取得します。
        /// </summary>
        public static ConsoleState DefaultNoLine => new(ConsoleColorSet.Default, false);

        /// <summary>
        /// 背景色を保持します。
        /// </summary>
        public ConsoleColor BackgroundColor { get; private set; } = backgroundColor;

        /// <summary>
        /// 前景色を保持します。
        /// </summary>
        public ConsoleColor ForegroundColor { get; private set; } = foregroundColor;

        /// <summary>
        /// 出力後に改行するかどうかを保持します。
        /// </summary>
        public bool WriteLine { get; private set; } = writeLine;

        /// <summary>
        /// 前景色と背景色を色セットとして取得または設定します。
        /// </summary>
        public ConsoleColorSet ColorSet
        {
            get => new(ForegroundColor, BackgroundColor);
            set
            {
                ForegroundColor = value.Foreground;
                BackgroundColor = value.Background;
            }
        }

        /// <summary>
        /// 指定された項目だけを現在の状態へ反映します。
        /// </summary>
        /// <param name="foregroundColor">変更する前景色。</param>
        /// <param name="backgroundColor">変更する背景色。</param>
        /// <param name="writeLine">変更する改行有無。</param>
        public void Update(ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null, bool? writeLine = null)
        {
            if (backgroundColor != null)
                BackgroundColor = backgroundColor.Value;

            if (foregroundColor != null)
                ForegroundColor = foregroundColor.Value;

            if (writeLine != null)
                WriteLine = writeLine.Value;
        }

        /// <summary>
        /// 現在の状態をコピーします。
        /// </summary>
        /// <returns>現在の状態と同じ内容を持つコピー。</returns>
        public ConsoleState Copy()
        {
            return new(BackgroundColor, ForegroundColor, WriteLine);
        }
    }
}
