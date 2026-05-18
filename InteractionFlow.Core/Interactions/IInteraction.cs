using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Interactions
{
    public interface IInteraction : IFlowNode
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.Interaction;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;

        Task<FlowEndToken> ExecuteAsync(IFlowContext context);
    }
}
