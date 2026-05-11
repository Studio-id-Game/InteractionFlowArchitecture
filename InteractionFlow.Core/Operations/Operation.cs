using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.OperationPorts;
using System;

namespace InteractionFlow.Core.Operations
{
    public abstract class Operation : IOperationPort
    {
        private readonly IFlowNode[] dependency;

        public Operation(params IFlowNode[] dependency)
        {
            this.dependency = dependency;
            ForceResetMemoryState();
        }

        public ReadOnlySpan<IFlowNode> Dependency => dependency;

        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Operation;

        public abstract void ForceResetMemoryState();
    }
}
