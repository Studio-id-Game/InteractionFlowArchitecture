using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ReactionPorts;
using System;

namespace InteractionFlow.Core.Reactions
{
    public abstract class Reaction(params IFlowNode[] dependency) : IReactionPort
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        public ReadOnlySpan<IFlowNode> Dependency => dependency;

        public abstract void ForceResetMemoryState();

        protected static FlowEndToken CreateFlowEndToken(IFlowContext context)
        {
            return new(context);
        }
    }
}
