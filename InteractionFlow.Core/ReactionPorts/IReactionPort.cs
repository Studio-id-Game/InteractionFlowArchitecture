using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.MultiFunctionPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ReactionPorts
{
    public interface IReactionPort : IFlowNodePortLayer
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;
    }

    public interface IReactionPort<in TOutput> : IReactionPort
    {
        public ValueTask ReactToUserAsync(IFlowContext context, TOutput reactionValue);
    }
}