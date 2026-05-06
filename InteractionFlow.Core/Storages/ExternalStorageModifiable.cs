using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.StoragePorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Storages
{
    public abstract class ExternalStorageModifiable<TValue, TStorage> : IStoragePortExternalModifiable<TValue>
        where TStorage : IStoragePortModifiable<TValue>
    {
        public ExternalStorageModifiable(TStorage cacheStorage)
        {
            CacheStorage = cacheStorage;
        }

        public TStorage CacheStorage { get; }

        public TValue? this[IFlowContext context]
        {
            get => CacheStorage[context];
            set => CacheStorage[context] = value;
        }

        public void ForceResetMemoryState()
        {
            CacheStorage.ForceResetMemoryState();
        }

        protected abstract Task<Result<TValue>> LoadFromPersistentCore(IFlowContext context);

        protected abstract Task<Result> SaveToPersistentCore(IFlowContext context, TValue value);

        public async Task<Result<TValue>> LoadFromPersistent(IFlowContext context)
        {
            var result = await LoadFromPersistentCore(context);

            if (result)
                CacheStorage[context] = result.Value;

            return result;
        }

        public Task<Result<TValue>> TryGetOrLoad(IFlowContext context)
        {
            if (TryGet(context, out var value))
            {
                return Task.FromResult(new Result<TValue>(value!));
            }

            return LoadFromPersistent(context);
        }

        public Task<Result> SaveToPersistent(IFlowContext context)
        {
            if (TryGet(context, out var value))
            {
                return SaveToPersistent(context, value!);
            }
            else
            {
                return Task.FromResult(Result.Error(new InvalidOperationException()));
            }
        }

        public async Task<Result> SaveToPersistent(IFlowContext context, TValue value)
        {
            var result = await SaveToPersistentCore(context, value);

            if (result)
            {
                CacheStorage[context] = value;
            }

            return result;
        }

        public bool TryGet(IFlowContext context, out TValue? value)
        {
            return CacheStorage.TryGet(context, out value);
        }

        public bool TryGetOrCreate(IFlowContext context, out TValue? value, Func<IFlowContext, (bool, TValue)> create)
        {
            return CacheStorage.TryGetOrCreate(context, out value, create);
        }

        public bool TryGetOrCreateDefault(IFlowContext context, out TValue? value)
        {
            return CacheStorage.TryGetOrCreateDefault(context, out value);
        }

        public bool TrySet(IFlowContext context, TValue? value)
        {
            return CacheStorage.TrySet(context, value);
        }
    }
}
