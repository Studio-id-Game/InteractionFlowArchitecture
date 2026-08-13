using InteractionFlow.Core.Entities.Architectures;

namespace InteractionFlow.Core.ExternalPorts.OperationPorts
{
    /// <summary>
    /// User による操作や入力の取得を担当する Operation ポートを表します。
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
