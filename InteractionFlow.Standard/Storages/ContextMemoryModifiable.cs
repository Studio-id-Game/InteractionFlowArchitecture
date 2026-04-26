using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Storages;
using InteractionFlow.Standard.Entities.Storages;
using System;
using System.Collections.Generic;

namespace InteractionFlow.Standard.Storages
{
    public class ContextMemoryModifiable<TContextKey, TValue> : MemoryStorageModifiable<TValue>
        where TValue : IContextMemoryValue<TContextKey>, new()
    {
        private readonly Dictionary<TContextKey, TValue> memory = new(EqualityComparer<TContextKey>.Default);

        public override TValue? this[IFlowContext context]
        {
            get
            {
                if (context.TryGet<TContextKey>(out var key) && key != null)
                {
                    return memory.GetValueOrDefault(key);
                }
                else
                {
                    return default;
                }
            }
            set
            {
                if (context.TryGet<TContextKey>(out var key) && key != null && value != null)
                {
                    memory[key] = value;
                }
                else if (key != null)
                {
                    memory.Remove(key);
                }
            }
        }

        public override TValue? TryGet(IFlowContext context)
        {
            if (context.TryGet<TContextKey>(out var key) && key != null)
            {
                if (memory.TryGetValue(key, out var value))
                {
                    return value;
                }

            }

            return default;
        }


        protected override TValue GetDefault(IFlowContext context)
        {
            if (context.TryGet<TContextKey>(out var key) && key != null)
            {
                var value = new TValue() { WithContextKey = key };
                value.AfterCreated(context);
                return value;
            }
            else
            {
                var value = new TValue();
                value.AfterCreated(context);
                return value;
            }
        }

        public override void ForceResetMemoryState()
        {
            foreach (var item in memory)
            {
                if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            memory.Clear();
            memory.TrimExcess();
        }

    }
}