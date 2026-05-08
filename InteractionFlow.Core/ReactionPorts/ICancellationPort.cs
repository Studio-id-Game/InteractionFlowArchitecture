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

        ValueTask<FlowEndToken> IExceptionPort<OperationCanceledException>.HandleExceptionAsync(IFlowContext context, OperationCanceledException exception)
        {
            return HandleCancellation(context, exception);
        }
    }
}
