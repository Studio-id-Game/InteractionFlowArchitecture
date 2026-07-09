using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.SerializerPorts;
using System.IO;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.SerializerPorts
{
    public interface INotepadDataSerializerPort : ISerializerPort<Stream, NotepadData>
    {

    }
}
