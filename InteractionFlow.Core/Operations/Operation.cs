using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.OperationPorts;
using System;

namespace InteractionFlow.Core.Operations
{
    public abstract class Operation(params IFlowNode[] dependency) : IOperationPort
    {
        public ReadOnlySpan<IFlowNode> Dependency => dependency;

        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Operation;

        public abstract void ForceResetMemoryState();
    }
}
