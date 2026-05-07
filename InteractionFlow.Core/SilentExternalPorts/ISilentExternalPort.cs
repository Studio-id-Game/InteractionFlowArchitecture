using InteractionFlow.Core.Entities.Architectures;

namespace InteractionFlow.Core.SilentExternalPorts
{
    public interface ISilentExternalPort : IFlowNodeStateful
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.SilentExternal;
    }
}
