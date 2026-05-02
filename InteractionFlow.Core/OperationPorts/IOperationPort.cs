using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.OperationPorts
{
    public interface IOperationPort : IFlowNodePortLayer
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Operation;
    }

    public interface IOperationPort<TInput> : IOperationPort
    {
        public ValueTask<TInput> OperateFromUserAsync(IFlowContext context);
    }
}
