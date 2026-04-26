using InteractionFlow.Core.ReactionPorts;

namespace InteractionFlow.Core.Reactions
{
    public abstract class Reaction : IReactionPort
    {
        protected Reaction()
        {
        }

        public virtual void ForceResetMemoryState()
        {
        }
    }
}