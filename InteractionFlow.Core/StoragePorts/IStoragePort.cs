using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;

namespace InteractionFlow.Core.StoragePorts
{
    public interface IStoragePort : IFlowNodePortLayer
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        object? this[IFlowContext context] { get; }

        bool TryGet(IFlowContext context, out object? value);
    }

    public interface IStoragePort<TValue> : IStoragePort
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        object? IStoragePort.this[IFlowContext context] => this[context];

        new TValue? this[IFlowContext context] { get; }

        bool IStoragePort.TryGet(IFlowContext context, out object? value)
        {
            var result = TryGet(context, out TValue? _value);
            value = _value;
            return result;
        }

        bool TryGet(IFlowContext context, out TValue? value);
    }
}
