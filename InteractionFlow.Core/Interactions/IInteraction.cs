using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Interactions
{
    public interface IInteraction : IFlowNode, IUserFlowInvoker
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.Interaction;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;

        public ValueTask<FlowEndToken> UseSystemFlowAsync(IFlowContext context);
    }
}