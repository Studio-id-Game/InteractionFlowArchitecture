using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.StoragePorts;
using System;

namespace InteractionFlow.Standard.StoragePorts
{
    public interface IStoragePortModifiable<TValue> : IStoragePort<TValue>
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        TValue? IStoragePort<TValue>.this[IFlowContext context]
        {
            get => this[context];
        }

        new TValue? this[IFlowContext context] { get; set; }

        bool TrySet(IFlowContext context, TValue? value);

        bool TryGetOrCreate(IFlowContext context, out TValue? value, Func<IFlowContext, (bool, TValue)> create);

        bool TryGetOrCreateDefault(IFlowContext context, out TValue? value);
    }
}
