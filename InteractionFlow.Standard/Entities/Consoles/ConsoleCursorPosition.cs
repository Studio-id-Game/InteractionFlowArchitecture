using InteractionFlow.Core.Entities.Architectures;

namespace InteractionFlow.Standard.Entities.Consoles
{
    /// <summary>
    /// コンソールカーソル位置を表します。未指定の座標は変更対象外として扱われます。
    /// </summary>
    /// <param name="left">左位置。<see langword="null"/> の場合は未指定。</param>
    /// <param name="top">上位置。<see langword="null"/> の場合は未指定。</param>
    public readonly struct ConsoleCursorPosition(int? left, int? top) : IFunctionState<ConsoleCursorPosition>
    {
        /// <summary>
        /// 左位置を取得します。
        /// </summary>
        public int? Left { get; } = left;

        /// <summary>
        /// 上位置を取得します。
        /// </summary>
        public int? Top { get; } = top;

        /// <summary>
        /// この値をコピーします。
        /// </summary>
        /// <returns>現在のカーソル位置。</returns>
        public ConsoleCursorPosition Copy()
        {
            return this;
        }
    }
}
