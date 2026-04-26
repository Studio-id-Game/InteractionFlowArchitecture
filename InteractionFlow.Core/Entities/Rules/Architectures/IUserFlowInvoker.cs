using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Entities.Rules.Architectures
{
    public interface IUserFlowInvoker
    {
        Task<FlowEndToken> ExecuteUserFlowAsync<TContext>(TContext context, IUserFlowHandler<TContext> handler)
           where TContext : IFlowContext;
    }
}
