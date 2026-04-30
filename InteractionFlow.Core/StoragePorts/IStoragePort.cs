using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.MultiFunctionPorts;

namespace InteractionFlow.Core.StoragePorts
{
    public interface IStoragePort : IFlowNodePortLayer
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;
    }

    public interface IStoragePort<TValue> : IStoragePort
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;
    }
}
