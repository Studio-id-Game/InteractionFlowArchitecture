namespace InteractionFlow.Standard.Entities.Consoles
{
    /// <summary>
    /// コンソールから入力された文字列を表します。
    /// </summary>
    /// <param name="text">入力された文字列。</param>
    public readonly struct ConsoleInputText(string text)
    {
        /// <summary>
        /// 入力された文字列を取得します。
        /// </summary>
        public readonly string text = text;
    }
}
