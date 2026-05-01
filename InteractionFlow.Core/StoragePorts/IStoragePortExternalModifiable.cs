using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using System.Threading.Tasks;

namespace InteractionFlow.Core.StoragePorts
{
    public interface IStoragePortExternalModifiable : IStoragePortExternal
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        Task<Result> SaveToPersistent(IFlowContext context);
    }

    public interface IStoragePortExternalModifiable<TValue> : IStoragePortExternal<TValue>, IStoragePortModifiable<TValue>, IStoragePortExternalModifiable
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Storage;

        Task<Result> SaveToPersistent(IFlowContext context, TValue value);
    }
}
