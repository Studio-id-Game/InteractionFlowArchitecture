using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.OperationPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Operations
{
    public abstract class Operation<TInput> : IOperationPort<TInput>
    {
        protected Operation()
        {
        }

        public virtual void ForceResetMemoryState()
        {
        }

        public abstract ValueTask<TInput> UserOperateAsync(IFlowContext context);
    }
}