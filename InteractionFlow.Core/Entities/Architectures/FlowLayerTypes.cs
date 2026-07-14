namespace InteractionFlow.Core.Entities.Architectures
{
    /// <summary>
    /// フローを構成するノードが属するレイヤーを表します。
    /// </summary>
    public enum FlowLayerTypes
    {
        /// <summary>
        /// レイヤーが指定されていない状態です。
        /// </summary>
        None = 0,

        /// <summary>
        /// SystemFlow レイヤーを表します。
        /// </summary>
        SystemFlow,

        /// <summary>
        /// Interaction レイヤーを表します。
        /// </summary>
        Interaction,

        /// <summary>
        /// 外部機能へのポートを表す FunctionPort レイヤーです。
        /// </summary>
        FunctionPort,

        /// <summary>
        /// FunctionPort の先にある外部機能の実装レイヤーです。
        /// </summary>
        FunctionExternal,
    }
}
