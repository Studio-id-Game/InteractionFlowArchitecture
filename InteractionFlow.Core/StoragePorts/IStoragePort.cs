using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;

namespace InteractionFlow.Core.StoragePorts
{
    public interface IStoragePort : IFlowNodeStateful
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;
    }

    public interface IStoragePort<TValue> : IStoragePort
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        TValue? this[IFlowContext context] { get; }

        bool TryGet(IFlowContext context, out TValue? value);
    }
}
