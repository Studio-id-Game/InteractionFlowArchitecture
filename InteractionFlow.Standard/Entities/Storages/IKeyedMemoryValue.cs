using InteractionFlow.Core.Entities.Contexts;

namespace InteractionFlow.Standard.Entities.Storages
{
    public interface IKeyedMemoryValue<TContextKey>
    {
        bool TryInitialize(IFlowContext context, TContextKey contextKey);
    }
}
