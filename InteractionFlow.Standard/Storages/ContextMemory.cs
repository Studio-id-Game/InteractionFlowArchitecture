using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Storages;
using InteractionFlow.Standard.Entities.Storages;
using System;
using System.Collections.Generic;

namespace InteractionFlow.Standard.Storages
{
    public class ContextMemory<TContextKey, TValue> : MemoryStorage<TValue>
        where TValue : IContextMemoryValue<TContextKey>, new()
    {
        private readonly Dictionary<TContextKey, TValue> memory = new(EqualityComparer<TContextKey>.Default);

        protected IEnumerable<TContextKey> CurrentKeys => memory.Keys;

        public override TValue? this[IFlowContext context]
        {
            get
            {
                if (context.TryGet<TContextKey>(out var key) && key != null)
                {
                    if (!memory.TryGetValue(key, out var value))
                    {
                        value = new() { WithContextKey = key };
                        value.AfterCreated(context);
                        memory[key] = value;
                    }

                    return value;
                }
                else
                {
                    return default;
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