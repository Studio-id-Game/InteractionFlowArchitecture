using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>親コンテキストから引き継ぐ値探索とキャンセル制御を指定します。</summary>
    [Flags]
    public enum ContextInheritance
    {
        /// <summary>親を探索せず、独自のキャンセル状態を使用します。</summary>
        None = 0,

        /// <summary>自身の追加値・独自値で見つからなかったとき、親を探索します。値はコピーしません。</summary>
        Values = 1,

        /// <summary>親の CancellationObject を共有します。</summary>
        Cancellation = 2,

        /// <summary>親の値探索とキャンセル状態を両方引き継ぎます。</summary>
        All = Values | Cancellation
    }
}
