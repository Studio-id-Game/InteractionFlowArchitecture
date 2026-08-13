using InteractionFlow.Core.Entities.Architectures;

namespace InteractionFlow.Core.ExternalPorts.SilentExternalPorts
{
    /// <summary>
    /// User との相互作用や System 内での記録を直接の目的とせず、
    /// 外部実行環境と連携する SilentExternal ポートを表します。
    /// </summary>
    public interface ISilentExternalPort : IFlowNodeStateful
    {
        /// <summary>
        /// SilentExternal ポートが FunctionPort レイヤーに属することを示します。
        /// </summary>
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        /// <summary>
        /// このノードが SilentExternal 種別の FunctionPort であることを示します。
        /// </summary>
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.SilentExternal;
    }
}
