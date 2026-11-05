using System;

namespace InteractionFlow.Standard.ConsoleSystem.Entities
{
    /// <summary>一回の出力に適用する色です。未指定の色は変更しません。</summary>
    public readonly struct ConsoleTextStyle
    {
        /// <summary>前景色と背景色を指定します。</summary>
        public ConsoleTextStyle(ConsoleColor? foreground = null, ConsoleColor? background = null)
        {
            Validate(foreground, nameof(foreground));
            Validate(background, nameof(background));
            Foreground = foreground;
            Background = background;
        }

        /// <summary>変更する前景色。</summary>
        public ConsoleColor? Foreground { get; }
        /// <summary>変更する背景色。</summary>
        public ConsoleColor? Background { get; }

        private static void Validate(ConsoleColor? color, string name)
        {
            if (color.HasValue && (color.Value < ConsoleColor.Black || color.Value > ConsoleColor.White))
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
