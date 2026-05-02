namespace InteractionFlow.Core.Entities.Contexts
{
    public interface IFlowContext : IFlowContextValue
    {
        public UserObject User { get; }

        public CancellationObject Cancellation { get; }
    }
}
