using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Storages;
using InteractionFlow.Standard.Entities.Storages;
using System;
using System.Collections.Generic;

namespace InteractionFlow.Standard.Storages
{
    public class KeyedMemoryReadonly<TContextKey, TValue> : StorageReadonly<TValue>
        where TValue : IKeyedMemoryValue<TContextKey>, new()
    {
        protected Dictionary<TContextKey, TValue> Dictionary { get; } = new(EqualityComparer<TContextKey>.Default);

        public override void ForceResetMemoryState()
        {
            foreach (var (_, value) in Dictionary)
            {
                if (value is IDisposable disposable)
                    disposable.Dispose();
            }
            Dictionary.Clear();
        }

        public override bool TryGet(IFlowContext context, out TValue? value)
        {
            if (!context.TryGet<TContextKey>(out var key) || key == null)
            {
                value = default;
                return false;
            }

            if (Dictionary.TryGetValue(key, out value))
            {
                return true;
            }

            value = new();

            if (value.TryInitialize(context, key))
            {
                Dictionary[key] = value;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
