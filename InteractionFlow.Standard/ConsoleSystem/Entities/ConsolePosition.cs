using System;

namespace InteractionFlow.Standard.ConsoleSystem.Entities
{
    /// <summary>左上を原点とするコンソール上の位置です。</summary>
    public readonly struct ConsolePosition
    {
        /// <summary>非負の座標を指定します。</summary>
        public ConsolePosition(int left, int top)
        {
            if (left < 0) throw new ArgumentOutOfRangeException(nameof(left));
            if (top < 0) throw new ArgumentOutOfRangeException(nameof(top));
            Left = left;
            Top = top;
        }

        /// <summary>左位置。</summary>
        public int Left { get; }
        /// <summary>上位置。</summary>
        public int Top { get; }
    }
}
