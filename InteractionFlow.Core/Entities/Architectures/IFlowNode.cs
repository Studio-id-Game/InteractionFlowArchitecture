namespace InteractionFlow.Core.Entities.Architectures
{
    /// <summary>
    /// SystemFlow、Interaction、FunctionPort など、フローを構成するノードを表します。
    /// </summary>
    public interface IFlowNode : IDependencyNode
    {
        /// <summary>
        /// ノードが属するフロー上のレイヤーを取得します。
        /// </summary>
        FlowLayerTypes Layer { get; }

        /// <summary>
        /// FunctionPort レイヤー内での機能種別を取得します。
        /// </summary>
        FunctionPortTypes FunctionTypes { get; }
    }
}
