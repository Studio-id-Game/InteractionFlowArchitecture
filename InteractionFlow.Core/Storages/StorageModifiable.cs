using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.StoragePorts;
using System;

namespace InteractionFlow.Core.Storages
{
    public abstract class StorageModifiable<TValue>(params IFlowNode[] dependency) : Storage<TValue>(dependency), IStoragePortModifiable<TValue>
    {
        public new TValue? this[IFlowContext context]
        {
            get => base[context];
            set => TrySet(context, value);
        }

        public abstract bool TrySet(IFlowContext context, TValue? value);

        protected abstract bool TryCreateDefault(IFlowContext context, out TValue? value);

        public bool TryGetOrCreate(IFlowContext context, out TValue? value, Func<IFlowContext, (bool, TValue)> create)
        {
            if (TryGet(context, out value))
                return true;

            var result = create(context);
            value = result.Item2;
            return result.Item1;
        }

        public bool TryGetOrCreateDefault(IFlowContext context, out TValue? value)
        {
            if (TryGet(context, out value))
                return true;

            return TryCreateDefault(context, out value);
        }
    }
}
