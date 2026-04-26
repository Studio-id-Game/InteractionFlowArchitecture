using InteractionFlow.Core.Entities.Rules.Architectures;

namespace InteractionFlow.Core.MultiFunctionPorts
{

    public interface IFlowNodePortLayer : IFlowNode, IMemoryState
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

    }
}