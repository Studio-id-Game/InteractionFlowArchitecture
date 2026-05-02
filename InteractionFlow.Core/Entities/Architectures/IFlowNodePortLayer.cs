namespace InteractionFlow.Core.Entities.Architectures
{

    public interface IFlowNodePortLayer : IFlowNode
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        void ForceResetMemoryState();
    }
}
