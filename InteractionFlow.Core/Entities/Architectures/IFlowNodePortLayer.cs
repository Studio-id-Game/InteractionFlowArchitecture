namespace InteractionFlow.Core.Entities.Rules.Architectures
{

    public interface IFlowNodePortLayer : IFlowNode
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        void ForceResetMemoryState();
    }
}
