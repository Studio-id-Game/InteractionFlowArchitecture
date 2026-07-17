namespace InteractionFlow.Core.Entities.Architectures
{
    /// <summary>
    /// Function 状態を一時変更するスコープを作成する拡張メソッドを提供します。
    /// </summary>
    public static class FunctionStateScopeExtension
    {
        /// <summary>
        /// 対象の現在状態を保存し、破棄時に復元する状態スコープを作成します。
        /// </summary>
        /// <typeparam name="TState">対象が保持する状態の型。</typeparam>
        /// <param name="target">状態スコープを作成する対象。</param>
        /// <returns>状態を一時変更するためのスコープ。</returns>
        public static FunctionStateScope<TState> GetStateScope<TState>(this IHasFunctionState<TState> target)
            where TState : IFunctionState<TState>
        {
            return new(target);
        }
    }
}
