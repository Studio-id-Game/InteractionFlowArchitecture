using InteractionFlow.Core.Entities.Architectures;

namespace InteractionFlow.Core.ExternalPorts.SilentExternalPorts
{
    /// <summary>
    /// ユーザーに直接観測されない、外部実行環境とのやりとりを担当する SilentExternal ポートを表します。
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
