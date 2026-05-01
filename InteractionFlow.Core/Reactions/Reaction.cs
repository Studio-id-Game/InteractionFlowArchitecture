using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ReactionPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Reactions
{
    public abstract class Reaction<TOutput> : IReactionPort<TOutput>
    {
        protected Reaction()
        {
        }

        public abstract ValueTask ReactToUserAsync(IFlowContext context, TOutput reactionValue);

        public virtual void ForceResetMemoryState()
        {
        }
    }
}
