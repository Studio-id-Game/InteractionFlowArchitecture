using InteractionFlow.Core.Entities.Rules.Architectures;

namespace InteractionFlow.Standard.Entities.Storages
{
    public interface IContextMemoryValue<TContextKey>
    {
        public TContextKey WithContextKey { set; }

        public void AfterCreated(IFlowContext context);
    }
}
