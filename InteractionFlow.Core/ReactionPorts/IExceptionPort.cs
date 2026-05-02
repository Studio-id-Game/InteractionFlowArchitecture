using InteractionFlow.Core.Entities.Architectures;
using System;

namespace InteractionFlow.Core.ReactionPorts
{
    public interface IExceptionPort : IReactionPort<Exception>
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        public bool ThrowException { get; set; }
    }
}
