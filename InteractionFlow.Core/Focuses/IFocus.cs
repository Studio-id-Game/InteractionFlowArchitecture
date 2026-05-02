using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Focuses
{
    public interface IFocus : IFlowNode
    {
        IEnumerable<IInteraction> Interactions { get; }

        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.Focus;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.None;
    }

    public interface IFocus<in TContext> : IFocus
        where TContext : IFlowContext
    {
        Task<FlowEndToken> FlowWithUserAsync(TContext context);
    }
}
