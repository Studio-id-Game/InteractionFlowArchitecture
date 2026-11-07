using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Entities;
using System.IO;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts
{
    public interface INotepadDataSerializerPort : ISerializerPort<Stream, NotepadData> { }
}
