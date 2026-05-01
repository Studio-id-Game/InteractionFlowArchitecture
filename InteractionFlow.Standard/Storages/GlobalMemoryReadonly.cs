using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Storages;

namespace InteractionFlow.Standard.Storages
{
    public class GlobalMemoryReadonly<TValue> : StorageReadonly<TValue>
        where TValue : new()
    {
        private TValue value = new();

        public override bool TryGet(IFlowContext context, out TValue? value)
        {
            value = this.value;
            return true;
        }

        public override void ForceResetMemoryState()
        {
            value = new();
        }
    }
}
