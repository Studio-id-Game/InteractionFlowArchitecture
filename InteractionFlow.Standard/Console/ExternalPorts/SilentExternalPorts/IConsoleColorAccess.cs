using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System;

namespace InteractionFlow.Standard.Console.ExternalPorts.SilentExternalPorts
{
    /// <summary>
    /// 現在のコンソール色を取得または変更する SilentExternal ポートを表します。
    /// </summary>
    public interface IConsoleColorAccess : ISilentRequestPort<(ConsoleColor foreground, ConsoleColor background), (ConsoleColor? foreground, ConsoleColor? background)>
    {
        /// <summary>
        /// 現在の前景色を取得または設定します。
        /// </summary>
        public ConsoleColor ForegroundColor { get; set; }

        /// <summary>
        /// 現在の背景色を取得または設定します。
        /// </summary>
        public ConsoleColor BackgroundColor { get; set; }
    }
}
