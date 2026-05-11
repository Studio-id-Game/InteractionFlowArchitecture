using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.SilentExternalPorts;
using System;

namespace InteractionFlow.Core.SilentExternals
{
    public abstract class SilentExternal : ISilentExternalPort
    {
        private readonly IFlowNode[] dependency;

        public SilentExternal(params IFlowNode[] dependency)
        {
            this.dependency = dependency;
            ForceResetMemoryState();
        }

        public ReadOnlySpan<IFlowNode> Dependency => dependency;

        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.SilentExternal;

        public abstract void ForceResetMemoryState();
    }
}
