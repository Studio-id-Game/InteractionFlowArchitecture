using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using System;

namespace InteractionFlow.Standard.Externals.ConsoleRule
{
    /// <summary>
    /// 現在のコンソール色を Function 状態として読み書きするアダプタです。
    /// </summary>
    public readonly struct ConsoleColorScope : IHasFunctionState<ConsoleColorSet>
    {
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
    }
}
