namespace InteractionFlow.Core.Entities.Architectures
{
    /// <summary>
    /// <see cref="IFlowNode"/> の基本情報を具象型から取得する拡張メソッドを提供します。
    /// </summary>
    /// <remarks>
    /// <see cref="IFlowNode"/> のメンバーは、default interface member や明示的なインターフェイス実装によって提供される場合があります。
    /// この拡張メソッド群は、具象型の変数からそれらのメンバーを簡潔に参照するための補助 API です。
    /// </remarks>
    public static class IFlowNodeExtensions
    {
        /// <summary>
        /// <see cref="IFlowNode.Name"/> として定義されたノードの表示名を取得します。
        /// </summary>
        /// <typeparam name="T">対象ノードの型。</typeparam>
        /// <param name="this">対象ノード。</param>
        /// <returns>ノードの表示名。</returns>
        public static string GetName<T>(this T @this) where T : IFlowNode => @this.Name;

        /// <summary>
        /// <see cref="IFlowNode.Layer"/> として定義された、ノードが属するフロー上のレイヤーを取得します。
        /// </summary>
        /// <typeparam name="T">対象ノードの型。</typeparam>
        /// <param name="this">対象ノード。</param>
        /// <returns>ノードのレイヤー種別。</returns>
        public static FlowLayerTypes GetLayer<T>(this T @this) where T : IFlowNode => @this.Layer;

        /// <summary>
        /// <see cref="IFlowNode.FunctionTypes"/> として定義された、FunctionPort レイヤー内での機能種別を取得します。
        /// </summary>
        /// <typeparam name="T">対象ノードの型。</typeparam>
        /// <param name="this">対象ノード。</param>
        /// <returns>ノードの FunctionPort 種別。</returns>
        public static FunctionPortTypes GetFunctionTypes<T>(this T @this) where T : IFlowNode => @this.FunctionTypes;
    }
}
