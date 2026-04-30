using InteractionFlow.Core.Entities.Rules.Architectures;

namespace InteractionFlow.Core.MultiFunctionPorts
{

    public interface IFlowNodePortLayer : IFlowNode
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        void ForceResetMemoryState();
    }
}
