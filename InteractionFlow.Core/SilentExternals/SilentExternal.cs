using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.SilentExternalPorts;
using System;

namespace InteractionFlow.Core.SilentExternals
{
    public abstract class SilentExternal(params IFlowNode[] dependency) : ISilentExternalPort
    {
        public ReadOnlySpan<IFlowNode> Dependency => dependency;

        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.SilentExternal;

        public abstract void ForceResetMemoryState();
    }
}
