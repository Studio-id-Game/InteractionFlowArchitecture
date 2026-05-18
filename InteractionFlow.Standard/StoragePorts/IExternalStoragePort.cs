using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.StoragePorts;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.StoragePorts
{
    public interface IExternalStoragePort<TValue> : IStoragePort<TValue>
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        Task<Result<TValue>> LoadFromPersistentAsync(IFlowContext context);

        Task<Result<TValue>> TryGetOrLoadAsync(IFlowContext context);
    }
}
