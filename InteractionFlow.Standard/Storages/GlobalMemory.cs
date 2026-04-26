using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Storages;
using System;

namespace InteractionFlow.Standard.Storages
{
    public class GlobalMemory<TValue> : MemoryStorage<TValue>
        where TValue : new()
    {
        private TValue? value;

        public Func<TValue>? Default { get; set; }

        public override TValue this[IFlowContext context]
        {
            get
            {
                value ??= (Default != null ? Default() : new TValue());
                return value;
            }
        }

        public override TValue? TryGet(IFlowContext context)
        {
            return value;
        }

        public override void ForceResetMemoryState()
        {
            if (value is IDisposable disposable)
            {
                disposable.Dispose();
            }

            value = default;
            Default = null;
        }
    }
}