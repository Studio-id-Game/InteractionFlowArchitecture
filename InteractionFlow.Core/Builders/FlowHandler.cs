using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Builders
{
    public sealed class FlowHandler<TContext>(ScopeHandler scope, IProgramFlow<TContext> focus) : IDisposable where TContext : IFlowContext
    {
        private ScopeHandler? scope = scope;
        private IProgramFlow<TContext>? focus = focus;

        public async Task<FlowEndToken> ExecuteAsync(TContext context)
        {
            var focus = this.focus ?? throw new InvalidOperationException();
            return await focus.ExecuteAsync(context);
        }

        public void Dispose()
        {
            scope?.Dispose();
            scope = null;
            focus = null;
        }
    }
}
