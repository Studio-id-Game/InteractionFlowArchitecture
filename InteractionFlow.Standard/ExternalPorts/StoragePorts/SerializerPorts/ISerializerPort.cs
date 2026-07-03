using InteractionFlow.Core.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.SerializerPorts
{
    public interface ISerializerPort<TData, TValue>
    {
        Task<Result<TData>> Serialize(Result<TValue> inputValue, Result<TData> refData);

        Task<Result<TValue>> Deserialize(Result<TData> inputData, Result<TValue> refValue);
    }
}
