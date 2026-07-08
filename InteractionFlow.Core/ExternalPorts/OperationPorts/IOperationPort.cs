using InteractionFlow.Core.Entities.Architectures;

namespace InteractionFlow.Core.ExternalPorts.OperationPorts
{
    /// <summary>
    /// ユーザー入力や外部条件の取得を担当する Operation ポートを表します。
    /// </summary>
    public interface IOperationPort : IFlowNodeStateful
    {
        /// <summary>
        /// Operation ポートが FunctionPort レイヤーに属することを示します。
        /// </summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        /// <summary>
        /// このノードが Operation 種別の FunctionPort であることを示します。
        /// </summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Operation;
    }
}
