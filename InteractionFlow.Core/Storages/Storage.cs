using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.StoragePorts;
using System;

namespace InteractionFlow.Core.Storages
{
    public abstract class Storage<TValue>(params IFlowNode[] dependency) : IStoragePort<TValue>
    {
        public ReadOnlySpan<IFlowNode> Dependency => dependency;

        public TValue? this[IFlowContext context]
        {
            get => TryGet(context, out var value) ? value : default;
        }

        public abstract bool TryGet(IFlowContext context, out TValue? value);

        public abstract void ForceResetMemoryState();
    }
}
