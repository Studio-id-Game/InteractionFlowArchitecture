using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ReactionPorts
{
    public interface ICancellationPort : IReactionPort, IExceptionPort<OperationCanceledException>
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        ValueTask<FlowEndToken> HandleCancellation(IFlowContext context, OperationCanceledException exception);
    }
}
