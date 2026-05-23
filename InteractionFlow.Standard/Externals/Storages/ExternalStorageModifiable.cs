using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.ExternalPorts.StoragePorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Storages
{
    public abstract class ExternalStorageModifiable<TValue, TStorage>(
        TStorage cacheStorage,
        params IFlowNode[] dependency)
        : ExternalStorage<TValue, TStorage>(cacheStorage, dependency),
        IExternalStoragePortModifiable<TValue>
        where TStorage : IStoragePortModifiable<TValue>
    {
        public new TValue? this[IFlowContext context]
        {
            get => base[context];
            set => CacheStorage[context] = value;
        }

        public async Task<Result> SaveToPersistentAsync(IFlowContext context, TValue value)
        {
            var result = await SaveToPersistentCoreAsync(context, value);

            if (result)
                CacheStorage[context] = value;

            return result;
        }

        public Task<Result> SaveToPersistentAsync(IFlowContext context)
        {
            return SaveToPersistentCoreAsync(context, CacheStorage[context]);
        }

        protected abstract Task<Result> SaveToPersistentCoreAsync(IFlowContext context, TValue? value);

        public new bool TryGet(IFlowContext context, out TValue? value)
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
