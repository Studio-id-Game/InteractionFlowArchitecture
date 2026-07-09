using InteractionFlow.Core.Entities.Architectures;

namespace InteractionFlow.Core.ExternalPorts.ReactionPorts
{
    /// <summary>
    /// ユーザーに観測可能な出力や終了時の反応を担当する Reaction ポートを表します。
    /// </summary>
    public interface IReactionPort : IFlowNodeStateful
    {
        /// <summary>
        /// Reaction ポートが FunctionPort レイヤーに属することを示します。
        /// </summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        /// <summary>
        /// このノードが Reaction 種別の FunctionPort であることを示します。
        /// </summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;
    }
}
