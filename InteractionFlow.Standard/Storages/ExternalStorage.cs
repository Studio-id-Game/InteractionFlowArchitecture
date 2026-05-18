using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.StoragePorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Storages
{
    public abstract class ExternalStorage<TValue, TStorage> : IExternalStoragePort<TValue>
        where TStorage : IStoragePortModifiable<TValue>
    {
        private readonly TStorage cacheStorage;
        private readonly IFlowNode[] dependency;

        public ExternalStorage(TStorage cacheStorage, params IFlowNode[] dependency)
        {
            this.cacheStorage = cacheStorage;
            this.dependency = dependency;
            ForceResetMemoryState();
        }

        public TStorage CacheStorage => cacheStorage;

        public ReadOnlySpan<IFlowNode> Dependency => (IFlowNode[])[cacheStorage, .. dependency];

        public TValue? this[IFlowContext context] => CacheStorage[context];

        public virtual void ForceResetMemoryState()
        {
            CacheStorage.ForceResetMemoryState();
        }

        protected abstract Task<Result<TValue>> LoadFromPersistentCoreAsync(IFlowContext context);

        public async Task<Result<TValue>> LoadFromPersistentAsync(IFlowContext context)
        {
            var result = await LoadFromPersistentCoreAsync(context);

            if (result)
                CacheStorage[context] = result.Value;

            return result;
        }

        public bool TryGet(IFlowContext context, out TValue? value)
        {
            return CacheStorage.TryGet(context, out value);
        }

        public Task<Result<TValue>> TryGetOrLoadAsync(IFlowContext context)
        {
            if (TryGet(context, out var value))
            {
                return Task.FromResult(new Result<TValue>(value!));
            }

            return LoadFromPersistentAsync(context);
        }
    }
}
