using InteractionFlow.Core.Entities.Contexts;

namespace InteractionFlow.Standard.Storages
{
    public class GlobalMemoryModifiable<TValue> : StorageModifiable<TValue>
    {
        private TValue? value;

        public override bool TryGet(IFlowContext context, out TValue? value)
        {
            value = this.value;
            return value != null;
        }

        public override bool TrySet(IFlowContext context, TValue? value)
        {
            this.value = value;
            return true;
        }

        protected override bool TryCreateDefault(IFlowContext context, out TValue? value)
        {
            value = default;
            return value != null;
        }

        public override void ForceResetMemoryState()
        {
            value = default;
        }
    }
}
