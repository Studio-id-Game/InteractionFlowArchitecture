using System;

namespace InteractionFlow.Core.ReactionPorts
{
    public interface ICancellationPort : IReactionPort<OperationCanceledException>
    {
        public bool ThroughCancellationException { get; set; }
    }
}