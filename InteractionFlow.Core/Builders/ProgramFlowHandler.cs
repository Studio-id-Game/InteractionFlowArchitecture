using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Builders
{
    public sealed class ProgramFlowHandler<TContext>(ScopeHandler scope, IProgramFlow<TContext> programFlow) : IDisposable where TContext : IFlowContext
    {
        private ScopeHandler? scope = scope;
        private IProgramFlow<TContext>? programFlow = programFlow;

        public async Task<FlowEndToken> ExecuteAsync(TContext context)
        {
            var programFlow = this.programFlow ?? throw new InvalidOperationException();
            return await programFlow.ExecuteAsync(context);
        }

        public void Dispose()
        {
            scope?.Dispose();
            scope = null;
            programFlow = null;
        }
    }
}
