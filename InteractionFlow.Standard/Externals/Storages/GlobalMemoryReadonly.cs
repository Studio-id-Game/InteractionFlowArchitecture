using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Storages;

namespace InteractionFlow.Standard.Externals.Storages
{
    public class GlobalMemoryReadonly<TValue> : Storage<TValue>
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
