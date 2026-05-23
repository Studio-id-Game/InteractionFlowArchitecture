using InteractionFlow.Core.Entities.Architectures;

namespace InteractionFlow.Core.ExternalPorts.ReactionPorts
{
    public interface IReactionPort : IFlowNodeStateful
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;
    }
}
