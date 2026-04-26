using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.StoragePorts;

namespace InteractionFlow.Core.Storages
{
    public abstract class MemoryStorageModifiable<TValue> : IMemoryStoragePortModifiable<TValue>
    {
        public abstract TValue? this[IFlowContext context] { get; set; }

        public TValue CreateDefault(IFlowContext context)
        {
            var value = GetDefault(context);
            this[context] = value;
            return value;
        }

        public TValue GetOrCreateDefault(IFlowContext context)
        {
            var value = this[context];
            if (value != null)
            {
                return value;
            }
            else
            {
                return CreateDefault(context);
            }
        }

        public abstract void ForceResetMemoryState();

        protected abstract TValue GetDefault(IFlowContext context);

        public abstract TValue? TryGet(IFlowContext context);
    }
}