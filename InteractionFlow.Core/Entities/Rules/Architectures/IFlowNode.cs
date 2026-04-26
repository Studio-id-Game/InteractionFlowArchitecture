namespace InteractionFlow.Core.Entities.Rules.Architectures
{
    public interface IFlowNode
    {
        public string Name => GetType().Name;

        public FlowLayerTypes Layer { get; }

        public FunctionPortTypes FunctionTypes { get; }
    }
}