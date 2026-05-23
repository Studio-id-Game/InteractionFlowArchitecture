using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ExternalPorts.ReactionPorts
{
    public interface IExceptionPort<in T> : IReactionPort
        where T : Exception
    {
        public bool ThrowException { get; set; }

        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        ValueTask<FlowEndToken> HandleExceptionAsync(IFlowContext context, T exception);
    }
}
