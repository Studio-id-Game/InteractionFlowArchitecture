using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.StoragePorts;

namespace InteractionFlow.Core.Storages
{
    public abstract class MemoryStorage<TValue> : IMemoryStoragePort<TValue>
    {
        public abstract TValue? this[IFlowContext context] { get; }

        public abstract void ForceResetMemoryState();

        public abstract TValue? TryGet(IFlowContext context);
    }
}