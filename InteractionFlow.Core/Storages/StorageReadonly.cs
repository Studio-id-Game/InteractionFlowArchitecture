using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.StoragePorts;

namespace InteractionFlow.Core.Storages
{
    public abstract class StorageReadonly<TValue> : IStoragePort<TValue>
    {
        public TValue? this[IFlowContext context]
        {
            get => TryGet(context, out var value) ? value : default;
        }

        public abstract bool TryGet(IFlowContext context, out TValue? value);

        public abstract void ForceResetMemoryState();
    }
}
