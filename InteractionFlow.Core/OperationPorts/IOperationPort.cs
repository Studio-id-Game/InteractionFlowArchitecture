using InteractionFlow.Core.Entities.Architectures;

namespace InteractionFlow.Core.OperationPorts
{
    public interface IOperationPort : IFlowNodeStateful
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Operation;
    }
}
