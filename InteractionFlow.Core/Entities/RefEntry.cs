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
        public new TValue? Value
        {
            get => base.Value;
            set => base.Value = value;
        }
    }
}
