using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Rules.Architectures;
using System.Threading.Tasks;

namespace InteractionFlow.Core.StoragePorts
{

    public interface IExternalStoragePortModifiable<TValue> : IExternalStoragePort<TValue>, IMemoryStoragePortModifiable<TValue>
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        Task<Result> SaveToPersistent(IFlowContext context);
    }
}
