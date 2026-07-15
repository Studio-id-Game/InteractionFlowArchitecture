using InteractionFlow.Standard.Entities.Consoles;
using System;

namespace InteractionFlow.Standard.Externals.ConsoleRule
{
    /// <summary>
    /// 現在のコンソール色を Function 状態として読み書きするアダプタです。
    /// </summary>
    public class ConsoleColorScope : IDisposable
    {
        private readonly ConsoleColor foregroundColor = Console.ForegroundColor;
        private readonly ConsoleColor backgroundColor = Console.BackgroundColor;

        /// <summary>
        /// 現在のコンソール色を取得または設定します。
        /// </summary>
        public ConsoleColorSet State
        {
            get => new(Console.ForegroundColor, Console.BackgroundColor);
            set
            {
                Console.ForegroundColor = value.Foreground;
                Console.BackgroundColor = value.Background;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
        }
    }
}
