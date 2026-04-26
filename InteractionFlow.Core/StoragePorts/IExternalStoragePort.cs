using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Rules.Architectures;
using System.Threading.Tasks;

namespace InteractionFlow.Core.StoragePorts
{
    public interface IExternalStoragePort<TValue> : IMemoryStoragePort<TValue>
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        Task<Result> LoadFromPersistent(IFlowContext context);
    }
}
