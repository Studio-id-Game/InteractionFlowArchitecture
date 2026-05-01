using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using System.Threading.Tasks;

namespace InteractionFlow.Core.StoragePorts
{
    public interface IStoragePortExternal : IStoragePort
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        Task<Result> LoadFromPersistent(IFlowContext context);
    }

    public interface IStoragePortExternal<TValue> : IStoragePort<TValue>, IStoragePortExternal
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        async Task<Result> IStoragePortExternal.LoadFromPersistent(IFlowContext context)
        {
            return (await LoadFromPersistent(context)).AsResult;
        }

        new Task<Result<TValue>> LoadFromPersistent(IFlowContext context);

        Task<Result<TValue>> TryGetOrLoad(IFlowContext context);
    }
}
