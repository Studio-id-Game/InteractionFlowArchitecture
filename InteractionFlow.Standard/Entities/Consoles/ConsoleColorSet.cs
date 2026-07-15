using InteractionFlow.Core.Entities.Architectures;
using System;

namespace InteractionFlow.Standard.Entities.Consoles
{
    /// <summary>
    /// コンソールの前景色と背景色の組を表します。
    /// </summary>
    /// <param name="foreground">前景色。</param>
    /// <param name="background">背景色。</param>
    public readonly struct ConsoleColorSet(ConsoleColor foreground, ConsoleColor background) : IFunctionState<ConsoleColorSet>
    {
        /// <summary>
        /// 標準のコンソール色セットを取得します。
        /// </summary>
        public static ConsoleColorSet Default { get; } = new ConsoleColorSet(ConsoleColor.Gray, ConsoleColor.Black);

        /// <summary>
        /// 前景色を取得します。
        /// </summary>
        public ConsoleColor Foreground { get; } = foreground;

        /// <summary>
        /// 背景色を取得します。
        /// </summary>
        public ConsoleColor Background { get; } = background;

        /// <summary>
        /// この値をコピーします。
        /// </summary>
        /// <returns>現在の色セット。</returns>
        public ConsoleColorSet Copy()
        {
            return this;
        }
    }
}
