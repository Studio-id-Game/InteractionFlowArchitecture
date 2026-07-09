namespace InteractionFlow.Standard.Entities
{
    /// <summary>
    /// Function が保持する現在の状態を取得または設定できることを表します。
    /// </summary>
    /// <typeparam name="TState">保持する状態の型。</typeparam>
    public interface IHasFunctionState<TState>
        where TState : IFunctionState<TState>
    {
        /// <summary>
        /// 現在の Function 状態を取得または設定します。
        /// </summary>
        TState State { get; set; }
    }
}
