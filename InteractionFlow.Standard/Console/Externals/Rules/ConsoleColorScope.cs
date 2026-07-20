using InteractionFlow.Standard.Console.Entities;
using System;

namespace InteractionFlow.Standard.Console.Externals.Rules
{
    /// <summary>
    /// 現在のコンソール色を Function 状態として読み書きするアダプタです。
    /// </summary>
    public class ConsoleColorScope : IDisposable
    {
        private readonly ConsoleColor foregroundColor = System.Console.ForegroundColor;
        private readonly ConsoleColor backgroundColor = System.Console.BackgroundColor;

        /// <summary>
        /// 現在のコンソール色を取得または設定します。
        /// </summary>
        public ConsoleColorSet State
        {
            get => new(System.Console.ForegroundColor, System.Console.BackgroundColor);
            set
            {
                System.Console.ForegroundColor = value.Foreground;
                System.Console.BackgroundColor = value.Background;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            System.Console.ForegroundColor = foregroundColor;
            System.Console.BackgroundColor = backgroundColor;
        }
    }
}
