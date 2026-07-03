using InteractionFlow.Core.Entities;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.SerializerPorts;
using System.IO;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Storages.Serializers
{
    public abstract class StreamSerializer<TValue> : ISerializerPort<Stream, TValue>
    {
        public abstract Task<Result<TValue>> Deserialize(Result<Stream> inputData, Result<TValue> refValue);
        public abstract Task<Result<Stream>> Serialize(Result<TValue> inputValue, Result<Stream> refData);
    }
}
