using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System.Threading.Tasks;

namespace InteractionFlow.Core.StoragePorts
{
    public interface IExternalStoragePortModifiable<TValue> : IExternalStoragePort<TValue>, IStoragePortModifiable<TValue>
    {
        FlowLayerTypes IFlowNode.Layer => FlowLayerTypes.FunctionPort;

        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        Task<Result> SaveToPersistent(IFlowContext context, TValue value);
    }
}
