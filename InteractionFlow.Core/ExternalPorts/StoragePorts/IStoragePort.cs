using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System.Collections.Generic;

namespace InteractionFlow.Core.ExternalPorts.StoragePorts
{
    public interface IStoragePort : IFlowNodeStateful
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        Result ClearAndDispose();

        Result ClearWithoutDispose();
    }

    public interface IStoragePort<TKey> : IStoragePort
    {
        Result<TKey> GetKey(IFlowContext context);

        bool ContainsKey(TKey key);

        Result RemoveAndDispose(TKey key);

        Result RemoveWithoutDispose(TKey key);
    }

    public interface IStoragePort<TKey, TValue> : IStoragePort<TKey>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>
    {
        Result<TValue> Get(TKey key);

        Result<TValue> GetOrCreate(TKey key);
    }
}
