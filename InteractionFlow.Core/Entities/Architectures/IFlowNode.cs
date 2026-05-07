using System;

namespace InteractionFlow.Core.Entities.Architectures
{
    public interface IFlowNode
    {
        string Name => GetType().Name;

        FlowLayerTypes Layer { get; }

        FunctionPortTypes FunctionTypes { get; }

        ReadOnlySpan<IFlowNode> Dependency { get; }
    }
}
