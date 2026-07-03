using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.StoragePorts;
using System;
using System.Collections;
using System.Collections.Generic;

namespace InteractionFlow.Core.Externals.Storages
{
    public abstract class Storage<TKey, TValue> : IStoragePort<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>
    {
        private readonly IFlowNode[] dependency;
        private readonly Dictionary<TKey, TValue> items;

        public Storage(IEqualityComparer<TKey>? comparer = null, params IFlowNode[] dependency)
        {
            items = new(comparer);
            this.dependency = dependency;
            ForceResetMemoryState();
        }

        public ReadOnlySpan<IFlowNode> Dependency => dependency;

        // IStoragePort<TKey, TValue>
        #region IStoragePort<TKey, TValue>

        public int Count => items.Count;

        public Result ClearWithoutDispose()
        {
            foreach (var (key, value) in items)
            {
                var canRemove = CanRemoveValue(key, value);
                if (!canRemove)
                {
                    return canRemove;
                }
            }

            items.Clear();
            return true;
        }

        public Result ClearAndDispose()
        {
            foreach (var (key, value) in items)
            {
                var canRemove = CanRemoveValue(key, value);
                if (!canRemove)
                {
                    return canRemove;
                }
            }

            foreach (var value in items.Values)
            {
                if (value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            items.Clear();
            return true;
        }

        public bool ContainsKey(TKey key)
        {
            return items.ContainsKey(key);
        }

        public virtual void ForceResetMemoryState()
        {
            ClearAndDispose();
        }

        public Result<TValue> Get(TKey key)
        {
            if (items.TryGetValue(key, out var value))
            {
                return value;
            }
            else
            {
                return new KeyNotFoundException(key?.ToString());
            }
        }

        public virtual Result<TKey> GetKey(IFlowContext context)
        {
            if (context.TryGet<TKey>(out var key) && key != null)
            {
                return key;
            }
            else
            {
                return new KeyNotFoundException(nameof(TKey));
            }
        }

        public Result<TValue> GetOrCreate(TKey key)
        {
            var getResult = Get(key);
            if (getResult)
            {
                return getResult;
            }

            var createResult = CreateNewValue(key);

            if (createResult)
            {
                items.Add(key, createResult.Value!);
                return createResult;
            }

            return createResult;
        }

        public Result RemoveWithoutDispose(TKey key)
        {
            if (items.TryGetValue(key, out var value))
            {
                var canRemove = CanRemoveValue(key, value);

                if (canRemove)
                {
                    items.Remove(key);
                }

                return canRemove;
            }
            else
            {
                return new KeyNotFoundException();
            }
        }

        public Result RemoveAndDispose(TKey key)
        {
            if (items.TryGetValue(key, out var value))
            {
                var canRemove = CanRemoveValue(key, value);

                if (canRemove)
                {
                    items.Remove(key);
                }

                if (value is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                return canRemove;
            }
            else
            {
                return new KeyNotFoundException();
            }
        }

        protected abstract Result CanRemoveValue(TKey key, TValue value);

        protected abstract Result<TValue> CreateNewValue(TKey key);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        #endregion
    }
}
