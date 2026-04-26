using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.StoragePorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Storages
{
    public abstract class ExternalStorageModifiable<TValue, TMemory> : IExternalStoragePortModifiable<TValue>
        where TMemory : IMemoryStoragePortModifiable<TValue>, new()
    {
        readonly TMemory memory;

        protected ExternalStorageModifiable()
        {
            memory = new();
        }

        public TValue? this[IFlowContext context]
        {
            get => memory[context];
            set => memory[context] = value;
        }

        public TValue CreateDefault(IFlowContext context)
        {
            return memory.CreateDefault(context);
        }

        public TValue GetOrCreateDefault(IFlowContext context)
        {
            return memory.GetOrCreateDefault(context);
        }

        public async Task<Result> LoadFromPersistent(IFlowContext context)
        {
            return await Load(context, value => memory[context] = value);
        }

        public async Task<Result> SaveToPersistent(IFlowContext context)
        {
            return await Save(context, memory[context]);
        }

        public virtual void ForceResetMemoryState()
        {
            memory.ForceResetMemoryState();
        }

        protected abstract Task<Result> Load(IFlowContext context, Action<TValue> set);

        protected abstract Task<Result> Save(IFlowContext context, TValue? value);

        public TValue? TryGet(IFlowContext context)
        {
            return memory.TryGet(context);
        }
    }
}