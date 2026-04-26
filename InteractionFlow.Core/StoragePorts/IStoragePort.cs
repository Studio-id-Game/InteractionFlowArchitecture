using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.MultiFunctionPorts;

namespace InteractionFlow.Core.StoragePorts
{
    public interface IStoragePort<TValue> : IFlowNodePortLayer
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;
    }
}
