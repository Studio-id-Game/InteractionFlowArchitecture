using InteractionFlow.Core.Entities.Rules.Architectures;
using System;

namespace InteractionFlow.Core.ReactionPorts
{
    public interface ICancellationPort : IReactionPort<OperationCanceledException>
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        public bool ThrowException { get; set; }
    }
}
