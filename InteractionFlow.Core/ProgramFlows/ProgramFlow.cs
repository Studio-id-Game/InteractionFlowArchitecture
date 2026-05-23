using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ProgramFlows
{
    public abstract class ProgramFlow(params IFlowNode[] dependency) : ProgramFlow<IFlowContext>(dependency)
    {
    }

    public abstract class ProgramFlow<TContext>(params IFlowNode[] dependency) : IProgramFlow<TContext>
        where TContext : IFlowContext
    {
        public ReadOnlySpan<IFlowNode> Dependency => dependency;

        public abstract Task<FlowEndToken> ExecuteAsync(TContext context);
    }
}
