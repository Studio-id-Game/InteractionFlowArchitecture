using InteractionFlow.Standard.Console.Entities;
using System;

namespace InteractionFlow.Standard.Console.Externals.Rules
{
    /// <summary>
    /// 現在のコンソール色を Function 状態として読み書きするアダプタです。
    /// </summary>
    public class ConsoleColorScope : IDisposable
    {
        private readonly ConsoleColor foregroundColor = global::System.Console.ForegroundColor;
        private readonly ConsoleColor backgroundColor = global::System.Console.BackgroundColor;

        /// <summary>
        /// 現在のコンソール色を取得または設定します。
        /// </summary>
        public ConsoleColorSet State
        {
            get => new(global::System.Console.ForegroundColor, global::System.Console.BackgroundColor);
            set
            {
                global::System.Console.ForegroundColor = value.Foreground;
                global::System.Console.BackgroundColor = value.Background;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            global::System.Console.ForegroundColor = foregroundColor;
            global::System.Console.BackgroundColor = backgroundColor;
        }
    }
}
