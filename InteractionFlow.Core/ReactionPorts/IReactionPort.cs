using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ReactionPorts
{
    public interface IReactionPort : IFlowNodePortLayer
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;
    }

    public interface IReactionPort<in TOutput> : IReactionPort
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        public ValueTask ReactToUserAsync(IFlowContext context, TOutput reactionValue);
    }
}
