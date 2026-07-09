using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.SerializerPorts;
using System.IO;

namespace InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.SerializerPorts
{
    public interface IUserSecureDataSerializerPort : ISerializerPort<Stream, UserSecureData>
    {

    }
}
