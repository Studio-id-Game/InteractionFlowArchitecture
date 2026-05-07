namespace InteractionFlow.Core.Entities.Architectures
{
    public interface IFlowNodeStateful : IFlowNode
    {
        void ForceResetMemoryState();
    }
}
