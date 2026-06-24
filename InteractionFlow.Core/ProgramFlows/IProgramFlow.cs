using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ProgramFlows
{
    public interface IProgramFlow : IFlowNode
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.ProgramFlow;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;
    }

    public interface IProgramFlow<in TContext> : IProgramFlow
        where TContext : IFlowContext
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.ProgramFlow;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;

        Task<FlowEndToken> ExecuteAsync(TContext context);
    }
}
