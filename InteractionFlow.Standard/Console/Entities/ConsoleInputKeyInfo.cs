using System;

namespace InteractionFlow.Standard.Console.Entities
{
    /// <summary>
    /// コンソールから入力されたキー情報を表します。
    /// </summary>
    /// <param name="key">入力されたキー情報。</param>
    public readonly struct ConsoleInputKeyInfo(ConsoleKeyInfo key)
    {
        /// <summary>
        /// 入力されたキー情報を取得します。
        /// </summary>
        public readonly ConsoleKeyInfo key = key;
    }
}
