using InteractionFlow.Core.Entities.Architectures;

namespace InteractionFlow.Core.Interactions
{
    /// <summary>
    /// Context 型に依存しない Interaction のメタデータを表します。
    /// </summary>
    /// <remarks>
    /// 実行には、要求する Context 型に対応する <see cref="IInteraction{TContext}"/> を使用します。
    /// </remarks>
    public interface IInteractionNode : IFlowNode
    {
        /// <summary>Interaction レイヤーに属することを示します。</summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.Interaction;

        /// <summary>FunctionPort 種別を持たないことを示します。</summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;
    }
}
