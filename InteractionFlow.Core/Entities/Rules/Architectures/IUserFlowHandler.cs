using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Entities.Rules.Architectures
{
    public interface IUserFlowHandler<in TContext>
           where TContext : IFlowContext
    {
        ValueTask<FlowEndToken> UserFlowCoreAsync(TContext context);
    }
}
