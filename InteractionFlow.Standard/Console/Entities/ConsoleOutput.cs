namespace InteractionFlow.Standard.Console.Entities
{
    /// <summary>
    /// コンソールへ出力する文字列を表します。
    /// </summary>
    /// <param name="text">出力する文字列。</param>
    public readonly struct ConsoleOutput(string text)
    {
        /// <summary>
        /// 出力する文字列を取得します。
        /// </summary>
        public readonly string text = text;
    }
}
