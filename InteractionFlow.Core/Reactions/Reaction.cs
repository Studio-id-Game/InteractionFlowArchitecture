using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ReactionPorts;
using System;

namespace InteractionFlow.Core.Reactions
{
    public abstract class Reaction : IReactionPort
    {
        private readonly IFlowNode[] dependency;

        private FlowEndToken? lastFlowEndToken;

        public Reaction(params IFlowNode[] dependency)
        {
            this.dependency = dependency;
            ForceResetMemoryState();
        }

        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        public ReadOnlySpan<IFlowNode> Dependency => dependency;

        public abstract void ForceResetMemoryState();

        protected FlowEndToken CreateFlowEndToken(IFlowContext context)
        {
            if (lastFlowEndToken == null || lastFlowEndToken.LastContext != context)
            {
                return lastFlowEndToken = new(context);
            }
            else
            {
                lastFlowEndToken.Exception = null;
                return lastFlowEndToken;
            }
        }
    }
}
