using InteractionFlow.Core.ExternalPorts.StoragePorts.SerializerPorts;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using System.IO;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.SerializerPorts
{
    public interface INotepadDataSerializerPort : ISerializerPort<Stream, NotepadData>
    {

    }
}
