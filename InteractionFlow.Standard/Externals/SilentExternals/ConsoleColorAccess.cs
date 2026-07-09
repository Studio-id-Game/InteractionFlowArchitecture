using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.SilentExternals
{
    /// <summary>
    /// 現在のコンソール色を取得または変更する標準 SilentExternal 実装です。
    /// </summary>
    public class ConsoleColorAccess : SilentRequest<(ConsoleColor foreground, ConsoleColor background), (ConsoleColor? foreground, ConsoleColor? background)>, IConsoleColorAccess
    {
        /// <summary>
        /// 現在の前景色を取得または設定します。
        /// </summary>
        public ConsoleColor ForegroundColor
        {
            get => Console.ForegroundColor;
            set => Console.ForegroundColor = value;
        }

        /// <summary>
        /// 現在の背景色を取得または設定します。
        /// </summary>
        public ConsoleColor BackgroundColor
        {
            get => Console.BackgroundColor;
            set => Console.BackgroundColor = value;
        }

        /// <summary>
        /// 指定された色だけを変更し、変更後の色セットを返します。
        /// </summary>
        /// <param name="context">実行時のフローコンテキスト。</param>
        /// <param name="arguments">変更する前景色と背景色。<see langword="null"/> の項目は変更しません。</param>
        /// <returns>変更後の前景色と背景色。</returns>
        public override ValueTask<(ConsoleColor foreground, ConsoleColor background)> ExecuteAsync(IFlowContext context, (ConsoleColor? foreground, ConsoleColor? background) arguments)
        {
            var (foreground, background) = arguments;

            if (foreground != null)
                ForegroundColor = foreground.Value;
            if (background != null)
                BackgroundColor = background.Value;

            return new((ForegroundColor, BackgroundColor));
        }

        /// <summary>
        /// この実装は保持状態を持たないため何もしません。
        /// </summary>
        public override void ForceResetMemoryState()
        {
        }
    }
}
