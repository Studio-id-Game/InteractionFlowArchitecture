using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.StoragePorts
{
    public interface IExternalStoragePort<TValue> : IStoragePort<TValue>
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        Task<Result<TValue>> LoadFromPersistent(IFlowContext context);

        Task<Result<TValue>> TryGetOrLoad(IFlowContext context);
    }
}
