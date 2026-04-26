using System;

namespace InteractionFlow.Core.ReactionPorts
{
    public interface IExceptionPort : IReactionPort<Exception>
    {
        public bool ThroughException { get; set; }
    }
}