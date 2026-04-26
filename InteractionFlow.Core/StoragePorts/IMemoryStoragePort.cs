using InteractionFlow.Core.Entities.Rules.Architectures;

namespace InteractionFlow.Core.StoragePorts
{
    public interface IMemoryStoragePort<TValue> : IStoragePort<TValue>
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        TValue? this[IFlowContext context] { get; }

        TValue? TryGet(IFlowContext context);
    }
}