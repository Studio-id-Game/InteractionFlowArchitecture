using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ProgramFlows
{
    public interface IFocus : IFlowNode
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.Focus;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;
    }

    public interface IFocus<in TContext> : IFocus
        where TContext : IFlowContext
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.Focus;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;

        Task<FlowEndToken> ExecuteAsync(TContext context);
    }
}
