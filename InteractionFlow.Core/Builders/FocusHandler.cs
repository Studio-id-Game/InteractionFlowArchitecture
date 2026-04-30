using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Focuses;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Builders
{
    public sealed class FocusHandler<TContext> : IDisposable where TContext : IFlowContext
    {
        private ScopeHandler? scope;
        private IFocus<TContext>? focus;

        public FocusHandler(ScopeHandler scope, IFocus<TContext> focus)
        {
            this.scope = scope;
            this.focus = focus;
        }

        public async Task<FlowEndToken> UseUserFlowAsync(TContext context)
        {
            var focus = this.focus ?? throw new InvalidOperationException();
            return await focus.FlowWithUserAsync(context);
        }

        public void Dispose()
        {
            scope?.Dispose();
            scope = null;
            focus = null;
        }
    }
}
