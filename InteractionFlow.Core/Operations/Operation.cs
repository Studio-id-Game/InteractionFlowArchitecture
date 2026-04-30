using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.OperationPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Operations
{
    public abstract class Operation<TInput> : IOperationPort<TInput>
    {
        public abstract ValueTask<TInput> OperateFromUserAsync(IFlowContext context);

        public virtual void ForceResetMemoryState()
        {
        }
    }
}
