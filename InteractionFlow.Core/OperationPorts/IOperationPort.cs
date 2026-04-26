using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.MultiFunctionPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.OperationPorts
{
    public interface IOperationPort : IFlowNodePortLayer
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Operation;
    }

    public interface IOperationPort<TInput> : IOperationPort
    {
        public ValueTask<TInput> UserOperateAsync(IFlowContext context);
    }
}