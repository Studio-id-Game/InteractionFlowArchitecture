using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.OperationPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Operations
{

    public class ValueOperation<TInput>(Func<ValueTask<TInput>> func) : IValueOperation<TInput>
    {
        public Func<ValueTask<TInput>> Func { get; } = func;

        public void ForceResetMemoryState()
        {
        }

        public ValueTask<TInput> OperateFromUserAsync(IFlowContext context)
        {
            context.Cancellation.GetToken().ThrowIfCancellationRequested();
            return Func();
        }
    }
}
