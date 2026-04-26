using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.StoragePorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Storages
{
    public abstract class ExternalStorage<TValue, TMemory> : IExternalStoragePort<TValue>
        where TMemory : IMemoryStoragePortModifiable<TValue>, new()
    {
        private readonly TMemory memory;

        protected ExternalStorage()
        {
            memory = new();
        }

        public TValue? this[IFlowContext context] => memory[context];

        public async Task<Result> LoadFromPersistent(IFlowContext context)
        {
            return await Load(context, value => memory[context] = value);
        }

        public virtual void ForceResetMemoryState()
        {
            memory.ForceResetMemoryState();
        }

        protected abstract Task<Result> Load(IFlowContext context, Action<TValue> set);

        public TValue? TryGet(IFlowContext context)
        {
            return memory.TryGet(context);
        }
    }
}