namespace InteractionFlow.Core.Entities
{
    /// <summary>
    /// 型指定による値の解決を提供する Entry の内部契約です。
    /// </summary>
    internal interface IEntry
    {
        /// <summary>
        /// 保持している値を指定した型として取得します。
        /// </summary>
        /// <typeparam name="T">取得する値の型。</typeparam>
        /// <returns>値を指定型として取得できる場合は成功結果。取得できない場合は失敗結果。</returns>
        public Result<T> Parse<T>();
    }
}
