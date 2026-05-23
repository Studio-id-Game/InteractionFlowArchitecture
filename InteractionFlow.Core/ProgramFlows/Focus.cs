using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ProgramFlows
{
    public abstract class Focus(params IFlowNode[] dependency) : Focus<IFlowContext>(dependency)
    {
    }

    public abstract class Focus<TContext>(params IFlowNode[] dependency) : IFocus<TContext>
        where TContext : IFlowContext
    {
        public ReadOnlySpan<IFlowNode> Dependency => dependency;

        public abstract Task<FlowEndToken> ExecuteAsync(TContext context);
    }
}
