namespace InteractionFlow.Core.Entities.Architectures
{
    /// <summary>
    /// <see cref="IFlowNode"/> の基本情報を取得する拡張メソッドを提供します。
    /// </summary>
    public static class IFlowNodeExtensions
    {
        /// <summary>
        /// ノードの表示名を取得します。
        /// </summary>
        /// <typeparam name="T">対象ノードの型。</typeparam>
        /// <param name="this">対象ノード。</param>
        /// <returns>ノードの表示名。</returns>
        public static string GetName<T>(this T @this) where T : IFlowNode => @this.Name;

        /// <summary>
        /// ノードが属するフロー上のレイヤーを取得します。
        /// </summary>
        /// <typeparam name="T">対象ノードの型。</typeparam>
        /// <param name="this">対象ノード。</param>
        /// <returns>ノードのレイヤー種別。</returns>
        public static FlowLayerTypes GetLayer<T>(this T @this) where T : IFlowNode => @this.Layer;

        /// <summary>
        /// FunctionPort レイヤー内での機能種別を取得します。
        /// </summary>
        /// <typeparam name="T">対象ノードの型。</typeparam>
        /// <param name="this">対象ノード。</param>
        /// <returns>ノードの FunctionPort 種別。</returns>
        public static FunctionPortTypes GetFunctionTypes<T>(this T @this) where T : IFlowNode => @this.FunctionTypes;
    }
}
