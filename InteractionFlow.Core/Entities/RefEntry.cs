namespace InteractionFlow.Core.Entities
{
    /// <summary>
    /// 値を外部から更新できる Entry です。
    /// </summary>
    /// <typeparam name="TValue">ラップする値の型。</typeparam>
    /// <param name="value">初期値。</param>
    public class RefEntry<TValue>(TValue? value) : Entry<TValue>(value)
    {
        /// <summary>
        /// ラップしている値を取得または設定します。
        /// </summary>
        /// <remarks>
        /// <see cref="Entry{TValue}.Value"/> は基底型では外部から設定できないため、
        /// 参照や状態更新を目的とする RefEntry では setter を公開するためにこのプロパティを再公開します。
        /// </remarks>
        public new TValue? Value
        {
            get => base.Value;
            set => base.Value = value;
        }
    }
}
