using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Storages;
using System;

namespace InteractionFlow.Standard.Storages
{
    public class GlobalMemoryModifiable<TValue> : MemoryStorageModifiable<TValue>
        where TValue : new()
    {
        private TValue? value;

        public Func<TValue>? Default { get; set; }

        public override TValue? this[IFlowContext context]
        {
            get => value;
            set => this.value = value;
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

        protected override TValue GetDefault(IFlowContext context)
        {
            return Default == null ? new() : Default();
        }
    }
}
