using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.MultiFunctionPorts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Interactions
{
    public interface IInteraction : IFlowNode
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.Interaction;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;

        IEnumerable<IFlowNodePortLayer> Ports { get; }

        Task<FlowEndToken> InteractWithUserAsync(IFlowContext context);
    }
}
