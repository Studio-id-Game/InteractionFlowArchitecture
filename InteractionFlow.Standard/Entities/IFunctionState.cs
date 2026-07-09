namespace InteractionFlow.Standard.Entities
{
    /// <summary>
    /// Function が一時的に差し替え可能な状態として扱う値を表します。
    /// </summary>
    /// <typeparam name="TSelf">コピー後も同じ型として扱う状態の型。</typeparam>
    public interface IFunctionState<TSelf>
    {
        /// <summary>
        /// 現在の状態を復元用にコピーします。
        /// </summary>
        /// <returns>現在の状態と同じ内容を持つコピー。</returns>
        public TSelf Copy();
    }
}
