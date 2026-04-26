using InteractionFlow.Core.Entities.Rules.Architectures;

namespace InteractionFlow.Core.StoragePorts
{

    public interface IMemoryStoragePortModifiable<TValue> : IMemoryStoragePort<TValue>
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        TValue? IMemoryStoragePort<TValue>.this[IFlowContext context]
        {
            get => this[context];
        }

        new TValue? this[IFlowContext context] { get; set; }

        TValue GetOrCreateDefault(IFlowContext context);

        TValue CreateDefault(IFlowContext context);
    }
}