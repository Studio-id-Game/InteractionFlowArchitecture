using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Focuses
{
    public interface IFocus : IFlowNode
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.Focus;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;
    }

    public interface IFocus<in TContext> : IFocus, IUserFlowHandler<TContext>
        where TContext : IFlowContext
    {
        public Task<FlowEndToken> UseUserFlowAsync(TContext context);
    }
}