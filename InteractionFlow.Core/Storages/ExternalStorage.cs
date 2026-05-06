using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.StoragePorts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Storages
{
    public abstract class ExternalStorage<TValue, TStorage> : IStoragePortExternal<TValue>
        where TStorage : IStoragePortModifiable<TValue>
    {
        protected ExternalStorage(TStorage cacheStorage)
        {
            CacheStorage = cacheStorage;
        }

        public TStorage CacheStorage { get; }

        public TValue? this[IFlowContext context] => CacheStorage[context];

        public void ForceResetMemoryState()
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
