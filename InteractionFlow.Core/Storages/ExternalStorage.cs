using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.StoragePorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Storages
{
    public abstract class ExternalStorage<TValue, TStorage>(TStorage cacheStorage, params IFlowNode[] dependency) : IExternalStoragePort<TValue>
        where TStorage : IStoragePortModifiable<TValue>
    {
        public TStorage CacheStorage => cacheStorage;

        public ReadOnlySpan<IFlowNode> Dependency => (IFlowNode[])[cacheStorage, .. dependency];

        public TValue? this[IFlowContext context] => CacheStorage[context];

        public virtual void ForceResetMemoryState()
        {
            CacheStorage.ForceResetMemoryState();
        }

        protected abstract Task<Result<TValue>> LoadFromPersistentCore(IFlowContext context);

        public async Task<Result<TValue>> LoadFromPersistent(IFlowContext context)
        {
            var result = await LoadFromPersistentCore(context);

            if (result)
                CacheStorage[context] = result.Value;

            return result;
        }

        public bool TryGet(IFlowContext context, out TValue? value)
        {
            return CacheStorage.TryGet(context, out value);
        }

        public Task<Result<TValue>> TryGetOrLoad(IFlowContext context)
        {
            if (TryGet(context, out var value))
            {
                return Task.FromResult(new Result<TValue>(value!));
            }

            return LoadFromPersistent(context);
        }
    }
}
