using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Standard.OperationPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Operations
{

    public class ValueOperation<TInput> : IValueOperation<TInput>
    {
        public ValueOperation(Func<ValueTask<TInput>> func)
        {
            Func = func;
        }

        public Func<ValueTask<TInput>> Func { get; }

        public void ForceResetMemoryState()
        {
        }

        public ValueTask<TInput> UserOperateAsync(IFlowContext context)
        {
            context.Cancellation.GetToken().ThrowIfCancellationRequested();
            return Func();
        }
    }
}
